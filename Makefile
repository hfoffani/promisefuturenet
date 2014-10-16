
LIBSTEST=-reference:NUnit.Framework,Microsoft.CSharp,PromiseFuture
MONO_PATH=/Library/Frameworks/Mono.framework/Libraries/mono/4.5/
NUNITCONSOLE="/Library/Frameworks/Mono.framework/Versions/Current/bin/nunit-console4"

all: test

clean:
	find . -name "*.dll" -delete


PromiseFuture.dll : PromiseFuture.cs
	mcs -target:library PromiseFuture.cs

PromiseFutureTest.dll : PromiseFutureTest.cs PromiseFuture.dll
	mcs -target:library $(LIBSTEST) -d:NUNIT PromiseFutureTest.cs 

test : PromiseFutureTest.dll
	MONO_PATH=$(MONO_PATH) $(NUNITCONSOLE) PromiseFutureTest.dll --nologo


