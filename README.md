## PromiseFutureNET ##

This is a couple of helpers I wrote to abstract
an asynchronous programming by loosely following
the Promise / Future model.

I've written these clases to help you translate
an ideal asynchronous process from English into
C#.

For instance, you might want to say: _The Future
will Bring me an integer, whose value is the result of i + 1,
Start working now (I'll tell you when I'd needed it.)_

Which translates to:

```c#
var i = 3;
var future = Future
    .Bring<int>(() => i + 1)
    .Start();
```

The previous expression will return immediately, and
you can do another work meanwhile. Once you reach to
a point where you need the integer value to proceed,
ask for it by calling `future.Value`. It will block
if the process haven't finished or return without
blocking if process had finished some time before.


### Use cases ###

See the test file for different use cases.


### How do I get set up? ###

This project was tested with version 3.10.0. It uses .NET 4.5 features.
You want to modify the Makefile changing the variables MONOPATH and NUNITCONSOLE and point to the Mono runtime and nunit-console respectively. 

Just run make and it will build and test the DLL. The Makefile is fairly simple so any make will do.


### Who do I talk to? ###

For questions or requests post an issue here or tweet me at
[@herchu](http://twitter.com/herchu)


