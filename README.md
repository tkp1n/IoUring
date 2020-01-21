![Badge](https://github.com/tkp1n/IoUring/workflows/.NET%20Core/badge.svg)
# IoUring

C# wrapper for `io_uring`

# IoUring.Transport

Experimental, managed ASP.NET Core Transport layer based on `io_uring`.

## Event loop

1. Accept new connections (1 syscall, until we get `IORING_OP_ACCEPT` with kernel v5.5)
2. Iterate over the connections managed by the thread and:
  * Prepare a read operation if required
  * Prepare a write operation if required
3. Submit all previously prepared operations to the kernel (1 syscall)
4. Handle all completed operations
5. Goto 1.

## Open issues in the implementation

- Error handling in general. This is currently a **very** minimal PoC.
- Polishing in general. Again, this is currently a **very** minimal PoC.
- Testing with more than a simple demo app...
- Benchmark and optimize
- Avoid burning CPU cycles in the event loop if no work has to be done.

## Room for improvement regarding `io_uring`:

- Create the largest possible (and reasonable) `io_uring`s. The max number of `entries` differs between kernel versions, perform auto-sensing.
- Implement `accept`-ing new connections using `io_uring`, once supported on non-rc kerne versions (v5.5). In the mean time at least meantime consider implementing something smarter than the `SpinWait` if a thread has no connections under its control.
- Profit from `IORING_FEAT_NODROP` or implement safety measures to ensure no more than `io_uring_params->cq_entries` operations are in flight at any given moment in time.
- Profit form `IORING_FEAT_SUBMIT_STABLE`. Currently the `iovec`s are allocated and fixed per connection to ensure they don't "move" during the execution of an operation.
- Profit from `io_uring_register` and `IORING_REGISTER_BUFFERS` to speed-up IO.

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