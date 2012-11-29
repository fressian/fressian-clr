(assembly-load-from "c:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.0/System.Numerics.dll")
(assembly-load-from "c:/Users/pairuser/dev/fressian-clr/src/clr/bin/Debug/fressian.dll")

(ns org.fressian.clr
  (:use [clojure.pprint :only [pprint]])
  (:require [org.fressian.generators :as gen]
            [clojure.walk :as walk]
            [clojure.data]
            [clojure.test.generative.generators :as tgen])
  (:import [System.IO MemoryStream]
           [org.fressian FressianWriter StreamingWriter FressianReader Writer Reader]
           [org.fressian.handlers WriteHandler ReadHandler WriteHandlerLookup]
           [org.fressian.impl ByteBufferInputStream BytesOutputStream]))

;;(set! *warn-on-reflection* true)

(defn as-write-lookup
  "Normalize ILookup or map into an ILookup."
  [o]
  (if (map? o)
    #_(reify |org.fressian.handlers.ILookup`2[System.Type,System.Collections.Generic.IDictionary`2[System.String,org.fressian.handlers.WriteHandler]]|
        (valAt [_ k] (get o k)))
    o
    o))

(defn as-read-lookup
  "Normalize ILookup or map into an ILookup."
  [o]
  (if (map? o)
    (reify |org.fressian.handlers.ILookup`2[System.Object,org.fressian.handlers.ReadHandler]|
        (valAt [_ k] (get o k)))
    o))

(defn ^Writer create-writer
  "Create a fressian writer targetting out. lookup can be an ILookup or
   a nested map of type => tag => WriteHandler."
  ;; TODO: make symmetric with create-reader, using io/output-stream?
  ([out] (create-writer out nil))
  ([out lookup]
     (FressianWriter. out (as-write-lookup lookup) true)))

(defn ^Reader create-reader
  "Create a fressian reader targetting in, which must be compatible
   with clojure.java.io/input-stream.  lookup can be an ILookup or
   a map of tag => ReadHandler."
  ([in] (create-reader in nil))
  ([in lookup] (create-reader in lookup true))
  ([in lookup validate-checksum]
     (FressianReader. in (as-read-lookup lookup) validate-checksum)))

(defn fressian
  "Fressian obj to output-stream compatible out.

   Options:
      :handlers    fressian handler lookup
      :footer      true to write footer"
  [out obj & {:keys [handlers footer]}]
  (with-open [os out]
    (let [writer (create-writer os handlers)]
      (.writeObject writer obj)
      (when footer
        (.writeFooter writer)))))

(defn defressian
  "Read single fressian object from input-stream-compatible in.

   Options:
      :handlers    fressian handler lookup
      :footer      true to validate footer"
  ([in & {:keys [handlers footer]}]
     (let [fin (create-reader in handlers)
           result (.readObject fin)]
       (when footer (.validateFooter fin))
       result)))

(defn bytestream->buf
  "given a MemoryStream, returns a byte array"
  [stream]
  (.ToArray stream))

(defn byte-buffer-seq
  "realizes a byte array into a vector"
  [bb]
  (into [] bb))

(defn byte-buf
  "fressians an object and returns the byte array.
   options are opts to (fressian ...)"
  [obj & options]
  (let [baos (MemoryStream.)]
    (apply fressian baos obj options)
    (bytestream->buf baos)))

(defn read-batch
  "Read a fressian reader fully (until eof), returning a (possibly empty)
   vector of results."
  [^Reader fin]
  (let [sentinel (Object.)]
    (loop [objects []]
      (let [obj (try (.readObject fin) (catch System.IO.EndOfStreamException e sentinel))]
        (if (= obj sentinel)
          objects
          (recur (conj objects obj)))))))

(def clojure-write-handlers
  {clojure.lang.Keyword
   {"key"
    (reify org.fressian.handlers.WriteHandler
      (write [_ w s]
        (.writeTag w "key" 2)
        (.writeObject w (namespace s))
        (.writeObject w (name s))))}
   clojure.lang.Symbol
   {"sym"
    (reify org.fressian.handlers.WriteHandler
      (write [_ w s]
        (.writeTag w "sym" 2)
        (.writeObject w (namespace s))
        (.writeObject w (name s))))}})

(def clojure-read-handlers
  {"key"
   (reify org.fressian.handlers.ReadHandler
     (read [_ rdr tag component-count]
       (keyword (.readObject rdr) (.readObject rdr))))
   "sym"
   (reify org.fressian.handlers.ReadHandler
     (read [_ rdr tag component-count]
       (symbol (.readObject rdr) (.readObject rdr))))
   "map"
   (reify org.fressian.handlers.ReadHandler
     (read [_ rdr tag component-count]
       (let [kvs (.readObject rdr)]
         (if (< (.Count kvs) 16)
           (clojure.lang.PersistentArrayMap. (.toArray kvs))
           (clojure.lang.PersistentHashMap/create (seq kvs))))))})

(def clojure-equality-delegate
  {"key"
   #(let [vls (into [] (.Value %))]
      (keyword (first vls) (second vls)))
   "sym"
   #(let [vls (into [] (.Value %))]
      (symbol (first vls) (second vls)))})

(defn roundtrip
  "Fressian and defressian o"
  ([o]
     (defressian (MemoryStream. (byte-buf o))))
  ([o write-handlers read-handlers]
     (defressian (MemoryStream. (byte-buf o :handlers write-handlers))
       :handlers read-handlers)))


;; for the bad classes that don't do value equality (e.g. float)
;; or don't work with data/diff (e.g. ByteBuffer)
(defprotocol EqualityDelegate
  (eqd [_] "nominate an object (usually this) to be used for equality comparison."))

(extend-protocol EqualityDelegate
  nil
  (eqd [n] n)
  
  org.fressian.TaggedObject
  (eqd [o]
    ((clojure-equality-delegate (.Tag o)) o)
    ;; {:org.fressian/tag (.Tag o)
    ;;  :org.fressian/value (into [] (.Value o))}
    )

  System.Text.RegularExpressions.Regex
  (eqd [p] (.ToString p))
  
  ;; System.Single
  ;; (eqd [f] (if (= Single/Nan f) ::float-nan f))

  ;; NOTE!!!! - clojrue-clr double hints seem to be broken
  ;; System.Double
  ;; (eqd [f] (if (= Double/NaN f) ::double-nan f))

  System.IO.MemoryStream
  (eqd [f] (into [] (.ToArray f)))
  
  Object
  (eqd [o] o))

;;FF - cannot extend float on clr!
;; CompilerException clojure.lang.CljCompiler.Ast.ParseException: Only long and double primitives are supported: f
;;    at clojure.lang.CljCompiler.Ast.FnMethod.Parse(FnExpr fn, ISeq form, Boolean isStatic) in D:\work\clojure-clr-1.4.1-fix\Clojure\Clojure\CljCompiler\Ast\FnMethod.cs:line 206
;;    at clojure.lang.CljCompiler.Ast.FnExpr.Parse(ParserContext pcon, ISeq form, String name) in D:\work\clojure-clr-1.4.1-fix\Clojure\Clojure\CljCompiler\Ast\FnExpr.cs:line 173
;; at clojure.lang.Compiler.AnalyzeSeq(ParserContext pcon, ISeq form, String name) in D:\work\clojure-clr-1.4.1-fix\Clojure\Clojure\CljCompiler\Compiler.cs:line 1558, compiling: (NO_SOURCE_PATH:6052)

;; (extend-type System.Single
;;   EqualityDelegate
;;   (eqd [f] (if (= Single/NaN f) ::float-nan f)))

;; work around limitations of walk/prewalk
(extend-type (class (float-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (double-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (byte-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (object-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (long-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (int-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(extend-type (class (boolean-array 0))
  EqualityDelegate
  (eqd [p] (into [] p)))

(defmacro assert=
  ([a b] `(assert= ~a ~b nil))
  ([a b context]
     `(let [a# (clojure.walk/prewalk eqd ~a) b# (clojure.walk/prewalk eqd ~b)]
        (when-not (= a# b#)
          (let [[d1# d2# s#] (clojure.data/diff a# b#)]
            (when (or d1# d2#)
              {:in-a d1# :in-b d2# :in-both s# :context ~context}))))))



;;; display protocol to help better view tagged objects during assertions
(defprotocol IDisplay
  (display [o]))

(extend-type System.Object
  IDisplay
  (display [o] o))

(extend-type org.fressian.TaggedObject
  IDisplay
  (display [o]
    {:tag (.Tag o)
     :value (into [] (.Value o))}))

(extend-type nil
  IDisplay
  (display [o] o))

(extend-type |System.Collections.Generic.List`1[System.Object]|
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (float-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (double-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (byte-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (object-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (long-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (int-array 0))
  IDisplay
  (display [o] (into [] o)))

(extend-type (class (boolean-array 0))
  IDisplay
  (display [o] (into [] o)))

(defmacro deftest-times
  "helper macro to create a test function that takes a [times] argument(number of times to run the test).  the generator is the data generator for use during the rountripping on each iteration"
  [name generator]
  `(defn ~name
     [times#]
     (map #(let [x# %1
                 i# (if (fn? ~generator) (~generator) ~generator)]
             (try
               (let [o# (roundtrip i# clojure-write-handlers clojure-read-handlers)]
                 {:iteraton x#
                  :input {:type (type i#) :val (eqd i#)}
                  :output {:type (type o#) :val (eqd o#)}
                  :result (assert= i# o#)})
               (catch Exception ex#
                   {:iteraton x#
                    :input {:type (type i#) :val (display i#)}
                    :output (.Message ex#)
                    :result :failed})))
          (range times#))))

(defn show-failures
  "helper function that will run test func and only pprint iterations that failed"
  [testfn iters]
  (pprint (filter #(or (not (nil? (:result %)))
                       (= :failed (:result %))
                       (= false (:result %)))
                  (testfn iters))))

(defn dump-failures
  "helper function that will run test func and dump iterations that failed to a stream"
  [testfn iters wtr]
  (let [d (pr-str (filter #(or (not (nil? (:result %)))
                               (= :failed (:result %))
                               (= false (:result %)))
                          (testfn iters)))]
    (.Write wtr d)))

(defn size
  "Measure the size of a fressianed object. Returns a map of
  :size, :second, :caching-size, and :cached-size.
  (:second will differ from size if there is internal caching.)"
  ([o] (size o nil))
  ([o write-handlers]
     (let [baos (MemoryStream.)
           writer (create-writer baos write-handlers)]
       (.writeObject writer o)
       (let [size (.Length baos)]
         (.writeObject writer o)
         (let [second (- (.Length baos) size)]
           (.writeObject writer o true)
           (let [caching-size (- (.Length baos) second size)]
             (.writeObject writer o true)
             {:size size
              :second second
              :caching-size caching-size
              #_:bytes #_(seq (.ToArray baos))
              :cached-size (- (.Length baos) caching-size second size)}))))))

(defn cache-session->fressian
  "write-args are a series of [fressianble cache?] pairs."
  [write-args]
  (let [baos (MemoryStream.)
        writer (create-writer baos)]
    (doseq [[idx [obj cache]] (map-indexed vector write-args)]
      (let [_ (.writeObject writer obj cache)])
      (when (= 39 (mod idx 40)) (.writeFooter writer)))
    (bytestream->buf baos)))

(defn roundtrip-cache-session
  "Roundtrip cache-session through fressian and back."
  [cache-session]
  (-> cache-session cache-session->fressian MemoryStream. create-reader read-batch))


;;; helper deftest fn for cached use case
(defmacro deftest-times-cached
  [name generator]
  `(defn ~name
     [times#]
     (map #(let [x# %1
                 i# (if (fn? ~generator) (~generator) ~generator)]
             (try
               (let [o# (roundtrip-cache-session i#)]
                 {:iteraton x#
                  :input {:type (type i#) :val (map first i#)}
                  :output {:type (type o#) :val o#}
                  :result (assert= (map first i#) o#)})
               (catch Exception ex#
                   {:iteraton x#
                    :input {:type (type i#) :val (display i#)}
                    :output (.Message ex#)
                    :result :failed})))
          (range times#))))

(defn compare-cache-and-uncached-versions
  "For each o in objects, print o, its uncached value, and its cached value.
   Used to verify cache skipping"
  [objects]
  (doseq [o objects]
    (println o
             " [Uncached:  "(byte-buffer-seq (cache-session->fressian [[o false]])) "]"
             " [Cached: " (byte-buffer-seq (cache-session->fressian [[o true]])) "]")))

;;;;;;;;;;;;;;;;;;;;;;;
;; test fns

(deftest-times test-fressian-character-encoding gen/single-char-string)
(deftest-times test-fressian-scalars gen/scalar)
(deftest-times test-fressian-builtins gen/fressian-builtin)
(deftest-times test-fressian-int-packing gen/longs-near-powers-of-2)
(deftest-times test-fressian-names gen/symbolic)

(deftest-times-cached test-fressian-strings-with-caching 
  (fn [] (gen/cache-session (tgen/string))))
(deftest-times-cached test-fressian-with-caching
  (fn [] (gen/cache-session (gen/fressian-builtin))))


(comment

  (show-failures test-fressian-character-encoding 1000)
  (show-failures test-fressian-scalars 10000)
  (show-failures test-fressian-builtins 1000)
  (show-failures test-fressian-int-packing 1)
  (show-failures test-fressian-names 1000)
  ;;TODO: size check
  (show-failures test-fressian-strings-with-caching 100)
  (show-failures test-fressian-with-caching 1)

  (with-open [wtr (System.IO.StreamWriter. "/Users/pairuser/tmp.clj")]
    (dump-failures test-fressian-with-caching 1 wtr))
  
)
