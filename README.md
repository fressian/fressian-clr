# Fressian CLR

This is a direct port of the Java Fressian Project to the CLR.

## What is Fressian? 

Fressian is an extensible binary data notation. It is inspired by, and
shares many design objectives with
[hessian](http://hessian.caucho.com/) and
[edn](https://github.com/edn-format/edn)

## Getting Started in Java

* Read the [Documentation](http://github.com/Datomic/fressian/wiki)

## CLR Implementation Notes
This is a CLR port of the [fressian](http://github.com/Datomic/fressian) 
project implemented for the JVM. This is the first iteration of the port, 
and the java source code was used as a basis to the code organization. 
Future efforts may involve making the API more idomatic to CLR developers.

### Platform Notes

#### Mapped Types
Below are the native fressian type to clr type mappings:

* int -> long (Int64)
* bool -> bool
* bytes -> byte[]
* double -> double
* float -> float
* string -> String
* list -> System.Collections.Generic.IList&lt;object&gt;
* set -> System.Collections.Generic.ISet&lt;object&gt;
* map -> System.Collections.Generic.IDictionary&lt;object, object&gt;
* uuid -> System.Guid
* regex -> System.Text.RegularExpressions.Regex
* uri -> System.Uri
* bigint -> System.Numerics.BigInteger
* bigdec -> System.Decimal (see BigDecimal support below)
* inst -> System.DateTime

#### BigDecimal Support
Since there is not a native BigDecimal equivilent on the CLR, only partial 
BigDecimal support was implemented in fressian-clr.  If the big decimal value
exceeds 96 bits (the size of the Decimal type in the CLR), an OverflowException
will be thrown.

### Examples

The ./test/apps/fressian-server project is a sample Fressian echo server.  It is
a test project that when run with no arguments, launches a Tcp Server on port 19876. 
When fressian-server.exe is passed a numeric argument [n] from the command line, 
it will act as a test client and transmit [n] number of random doubles to the 
fressian echo server.

The protocol of the fressian echo server is as follows:

* client sends a big-endian 64 bit long value indicating the number of fressian objects 
that are going to be written to the socket
* server responds with the big-endian 64 bit long that was sent
* client sends the same number of fressian objects as initially indicated
* server reads all objects, and then will echo them back to the client 

### Tests

The java fressian tests were all written using [test.generative 0.1.4](https://github.com/clojure/test.generative). 
A CLR port of [data.generators](https://github.com/clojure/data.generators) called 
[data.generators-clr](https://github.com/ffailla/data.generators-clr) was used 
for test data when porting these fressian tests.  

For these tests, [clojure-clr 1.4.1](https://github.com/clojure/clojure-clr) was used 
and is directly reference in the bin/repl.bat file.  The fressian project must first 
be compiled in orderfor the ./script/runtests.clj script to run from this repl, since it needs to load the fressian.dll assembly first.

## License

Copyright Metadata Partners, LLC.

Licensed under the EPL. (See the file epl.html.)