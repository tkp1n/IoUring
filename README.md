# IoUring

C# wrapper for [`io_uring`](https://kernel.dk/io_uring.pdf). This library fulfills the same purpose as the native [liburing](https://github.com/axboe/liburing), by which it is heavily inspired.
The primary goal of this library is to bring `io_uring` to all systems supporting it, also the ones without `liburing` pre-installed.

## Setting proper resource limits (`RLIMIT_MEMLOCK`)

If `ulimit -l` returns something along the lines of 64K, adjustments should be made.
It's simplest (although not smartest) to set `memlock` to unlimited in [limits.conf](https://linux.die.net/man/5/limits.conf) (e.g. Ubuntu), to set `DefaultLimitMEMLOCK=infinity` in [systemd config](https://jlk.fjfi.cvut.cz/arch/manpages/man/systemd-system.conf.5) (e.g. Clear Linux*), or to do the equivalent for your distro...

# IoUring.Transport

Experimental, managed ASP.NET Core Transport layer based on `io_uring`. This library is inspired by [kestrel-linux-transport](https://github.com/redhat-developer/kestrel-linux-transport/), a similar linux-specific transport layer based  on `epoll`.

This transport layer currently only supports the server scenario ([`IConnectionListenerFactory`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.connections.iconnectionlistenerfactory?view=aspnetcore-3.1)) but will eventually support the client scenario ([`IConnectionFactory`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.connections.iconnectionfactory?view=aspnetcore-3.1)) as well.

## Design

### Preparation

A (one day) configurable number of `TransportThread`s are started. Each thread opens an accept-socket on the server endpoint (IP and port) using the `SO_REUSEPORT` option. This allows all threads to accept connections and will let the kernel load balance between the accept-sockets.

All threads are provided with the writing end of the same `Channel` to write accepted connections to. This `Channel` will be read from when `ConnectAsync` is invoked on the `IConnectionListener`. The `Channel` is unbounded and back-pressure to temporarily disable `accept`ing new connections is not yet supported.  

Each thread creates an `io_uring` to schedule IO operations and to get notified of their completion.

Each thread also creates an `eventfd` in the semaphore-mode (`EFD_SEMAPHORE`) with an initial value of 0 and places a `readv` operation (`IORING_OP_READV`) from that `eventfd` onto the `io_uring`. This allows us - as we shall later see and use - to unblock the thread using a normal `write` to the `eventfd` if the thread is blocked by a `io_uring_enter` syscall waiting for an IO operation to complete. This could also be achieved by sending a no-op (`IORING_OP_NOP`) through the `io_uring` but that would require synchronization of access to said ring, as now multiple threads may be writing to it. This trick allows us to be entirely lock-free in event loop. (The only exception being the `Channel` mentioned above - which is a locking data structure).

### Event loop

Before the event loop is started, we place above-mentioned `readv` from the `eventfd` as well as a `poll` (`IORING_OP_POLL_ADD`) for acceptable connections (`POLLIN`) on the accept-socket.

The event loop is then made up of the following actions:

1. Check the read-poll-queue. This `ConcurrentQueue` contains connections that could be read from again, after a `FlushAsync` to the application completed **asynchronously** indicating that there was a need for back-pressure. The synchronous case is handled with a fast-path below. For each connection in this queue, a `poll` (`IORING_OP_POLL_ADD`) for incoming bytes (`POLLIN`) is added to the `io_uring`.
2. Check the write-poll-queue. This `ConcurrentQueue` contains connections that should be written to, after a `ReadAsync` from the application completed **asynchronously**. The synchronous case is handled with a fast-path below. For each connection in this queue, a `poll` (`IORING_OP_POLL_ADD`) for "writability" (`POLLOUT`) is added to the `io_uring`.
3. Submit all previously prepared operations to the kernel and block until at least on operation completed. (This involves one syscall to `io_uring_enter`).
4. Handle all completed operations. Typically each (successfully) completed operation causes another operation to be prepared for submission in the next iteration of the event loop. Recognized types of completed operations are:

* **eventfd poll completion**: The `poll` for the `eventfd` completed. This indicates that a `ReadAsync` from or a `FlushAsync` to the application completed asynchronously and that the corresponding connection was added to one of the above mentioned queues. The immediate action taken is to prepare another `poll` (`IORING_OP_POLL_ADD`) for the `eventfd`, as the connection specific `poll`s are added when handling the queues at the beginning of the next event loop iteration. This ensures that the transport thread could again be unlocked, if the next `io_uring_enter` blocks.
* **accept poll completion**: The `poll` for the accept-socket completed. This indicates that one or more connection could be `accept`ed. One connection is accepted by invoking the syscall `accept`. In a future release, this could be done via the `io_uring` (`IORING_OP_ACCEPT`) to avoid the syscall, but this feature will only be available in the kernel version 5.5. that is unreleased by the time of writing this. The accepted connection is added to the above-mentioned channel and two operations are triggered. A `poll` (`IORING_OP_POLL_ADD`) for incoming bytes (`POLLIN`) is added to the `io_uring` and a `ReadAsync` from the application is started to get bytes to be sent. If the latter completes synchronously, a `poll` (`IORING_OP_POLL_ADD`) for "writability" (`POLLOUT`) is added to the `io_uring` directly. In the asynchronous case, a callback is scheduled that will register the connection with the write-poll-queue and unblock the transport thread if necessary by writing to the `evetfd`.
* **read poll completion**: The `poll` for available data (`POLLIN`) on a socket completed. A `readv` (`IORING_OP_READV`) is added to the `io_uring` to read the data from the socket.
* **write poll completion**: The `poll` for "writability" (`POLLOUT`) of a socket completed. A `writev`(`IORING_OP_WRITEV`) for the data previously acquired during a `ReadAsync` is added to the `io_uring`.
* **read completion**: The `readv` previously added for the affected socket completed. The `Pipeline` is advanced past the number of bytes read and handed over to the application using `FlushAsync`. If `FlushAsync` completes synchronously, a `poll` (`IORING_OP_POLL_ADD`) for incoming bytes (`POLLIN`) is added to the `io_uring` directly. In the asynchronous case, a callback is scheduled that will register the connection with the read-poll-queue and unblock the transport thread if necessary by writing to the `evetfd`.
* **write completion**: The `writev` previously added for the affected socket completed. The `Pipeline` is advanced past the number of bytes written and more data from the application is read using `ReadAsync`. If `ReadAsync` completes synchronously, a `poll` for "writability" (`POLLOUT`) is added to the `io_uring` directly. In the asynchronous case, a callback is scheduled that will register the connection with the write-poll-queue and unblock the transport thread if necessary by writing to the `evetfd`.

### Use of `sqe->user_data` to keep context without allocations

Once an IO operation handed over to `io_uring` completes, the application needs to restore some contextual information regarding the operation that completed. This includes:

1. The type of operation that completed (listed in **bold** above).
2. The socket (and associated data) the operation was performed on

`io_uring` allows for 64-bit of user data to be provided with each submission, that will be routed through to the completion of the request. The lower 32-bit of this value are set to the socket file descriptor, the operation is performed on and the high 32-bit are set to an operation indicator. This ensures context can be restored after the completion of an asynchronous operation.

The socket file descriptor is used as index into a `Dictionary` to fetch the data associated with the socket.

## Open issues in the implementation

* Error handling in general. This is currently a **very** minimal PoC.
* Polishing in general. Again, this is currently a **very** minimal PoC.
* Testing with more than a simple demo app...
* Ensure this transport isn't added on systems with a kernel version <5.4
* Benchmark and optimize
* Enable CPU affinity
* Investigate whether the use of zero-copy options are profitable (vis-a-vis registered buffers)
* Use multi-`iovec` `readv`s if more than `_memoryPool.MaxBufferSize` bytes are readable and ensure that the syscall to `ioctl(_fd, FIONREAD, &readableBytes)` is avoided in the typical cases where one `iovec` is enough.

## Room for improvement regarding the use of `io_uring`

* Create the largest possible (and reasonable) `io_uring`s. The max number of `entries` differs between kernel versions, perform auto-sensing.
* Implement `accept`-ing new connections using `io_uring`, once supported on non-rc kerne versions (v5.5).
* Profit from `IORING_FEAT_NODROP` or implement safety measures to ensure no more than `io_uring_params->cq_entries` operations are in flight at any given moment in time.
* Profit form `IORING_FEAT_SUBMIT_STABLE`. Currently the `iovec`s are allocated and fixed per connection to ensure they don't "move" during the execution of an operation.
* Profit from `io_uring_register` and `IORING_REGISTER_BUFFERS` to speed-up IO.

## Try it out

Add the following MyGet feed to your nuget.config:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="myget-tkp1n" value="https://www.myget.org/F/tkp1n/api/v3/index.json" />
  </packageSources>
</configuration>
```
