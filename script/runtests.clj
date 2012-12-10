(ns runtests
  (:use [org.fressian.clr]))

(assembly-load-from "c:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.0/System.Numerics.dll")
(assembly-load-from "./src/clr/bin/Debug/fressian.dll")

(show-failures test-fressian-character-encoding 1000)
(show-failures test-fressian-scalars 10000)
(show-failures test-fressian-builtins 1000)
(show-failures test-fressian-int-packing 1)
(show-failures test-fressian-names 1000)
(show-failures test-fressian-strings-with-caching 100)
(show-failures test-fressian-with-caching 100)

(comment
  
  (show-failures test-fressian-character-encoding-socket 1000 "127.0.0.1" 19876)
  (show-failures test-fressian-scalars-socket 1000 "127.0.0.1" 19876)
  (show-failures test-fressian-builtins-socket 100 "127.0.0.1" 19876)
  (show-failures test-fressian-int-packing-socket 1 "127.0.0.1" 19876)
  (show-failures test-fressian-names-socket 1000 "127.0.0.1" 19876)

  ;; test caching
  (show-failures test-fressian-character-encoding-socket 1000 true  "127.0.0.1" 19876)
  (show-failures test-fressian-scalars-socket 10 true "127.0.0.1" 19876)
  (show-failures test-fressian-builtins-socket 100 true "127.0.0.1" 19876)
  (show-failures test-fressian-int-packing-socket 1 true "127.0.0.1" 19876)
  (show-failures test-fressian-names-socket 1000 true "127.0.0.1" 19876)

  (show-failures test-fressian-strings-with-caching-socket 10 "127.0.0.1" 19876)
  (show-failures test-fressian-with-caching-socket 10 "127.0.0.1 19876")

  )