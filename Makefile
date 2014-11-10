
LIBSTEST=-reference:NUnit.Framework,Microsoft.CSharp,PromiseFuture
MONOPATH=/Library/Frameworks/Mono.framework/Libraries/mono/4.5/
NUNITCONSOLE="/Library/Frameworks/Mono.framework/Versions/Current/bin/nunit-console4"

all: test

clean:
	find . -name "*.dll" -delete


PromiseFuture.dll : PromiseFuture.cs
	mcs -target:library PromiseFuture.cs

TestPromiseFuture.dll : TestPromiseFuture.cs PromiseFuture.dll
	mcs -target:library $(LIBSTEST) -d:NUNIT TestPromiseFuture.cs 

test : TestPromiseFuture.dll
	MONO_PATH=$(MONOPATH) $(NUNITCONSOLE) TestPromiseFuture.dll --nologo


