:warning: The IoUring.Transport subproject lives in it's own repositrory now: [tkp1n/IoUring.Transport](https://github.com/tkp1n/IoUring.Transport)

# IoUring

C# wrapper for [`io_uring`](https://kernel.dk/io_uring.pdf). This library fulfills the same purpose as the native [liburing](https://github.com/axboe/liburing), by which it is heavily inspired.
The primary goal of this library is to bring `io_uring` to all systems supporting it, also the ones without `liburing` pre-installed.

## Setting proper resource limits (`RLIMIT_MEMLOCK`)

If `ulimit -l` returns something along the lines of 64K, adjustments should be made.
It's simplest (although not smartest) to set `memlock` to unlimited in [limits.conf](https://linux.die.net/man/5/limits.conf) (e.g. Ubuntu), to set `DefaultLimitMEMLOCK=infinity` in [systemd config](https://jlk.fjfi.cvut.cz/arch/manpages/man/systemd-system.conf.5) (e.g. Clear Linux*), or to do the equivalent for your distro...

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
