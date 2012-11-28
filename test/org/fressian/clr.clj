(assembly-load-from "c:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.0/System.Numerics.dll")
(assembly-load-from "c:/Users/pairuser/dev/fressian-clr/src/clr/bin/Debug/fressian.dll")

(ns org.fressian.clr
  (:use ;;[clojure.test.generative]
        [clojure.pprint :only [pprint]])
  (:require [org.fressian.generators :as gen])
  (:import [org.fressian FressianWriter StreamingWriter FressianReader Writer Reader]
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
  [stream]
  (.ToArray stream))

(defn byte-buffer-seq
  [bb]
  (into [] bb))

(defn byte-buf
  [obj & options]
  (let [baos (System.IO.MemoryStream.)]
    (apply fressian baos obj options)
    (bytestream->buf baos)))

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


(defn roundtrip
  "Fressian and defressian o"
  ([o]
     (defressian (System.IO.MemoryStream. (byte-buf o))))
  ([o write-handlers read-handlers]
     (defressian
        (System.IO.MemoryStream.
         (byte-buf o :handlers write-handlers))
        :handlers read-handlers
        )))

(defprotocol IEquality
  (equals [a b]))

(extend-type org.fressian.TaggedObject
  IEquality
  (equals [a b]
    ;;(prn (into [] (.Value a)) b)
    (= b (cond
          (= (.Tag a) "key") (apply keyword (.Value a))
          (= (.Tag a) "sym") (apply symbol (.Value a))
          :else nil))))

(extend-type System.Text.RegularExpressions.Regex
  IEquality
  (equals [a b] (= (.ToString a) (.ToString b))))

(extend-type System.IO.MemoryStream
  IEquality
  (equals [a b] (= (into [] (.ToArray a))
                   (into [] (.ToArray b)))))

(extend-type System.Object
  IEquality
  (equals [a b] (= a b)))

(extend-type (class (float-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (double-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (byte-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (object-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (long-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (int-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type (class (boolean-array 0))
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))

(extend-type nil
  IEquality
  (equals [a b] (and (nil? a) (nil? b))))

(extend-type |System.Collections.Generic.List`1[System.Object]|
  IEquality
  (equals [a b]
    (= (into [] a) (into [] b))))


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
  (display [o]
    (into [] o)))


(defmacro deftest-times
  [name generator]
  `(defn ~name
     [times#]
     (map #(let [x# %1
                 i# (if (fn? ~generator) (~generator) ~generator)]
             (try
               (let [o# (roundtrip i# clojure-write-handlers clojure-read-handlers)]
                 {:iteraton x#
                  :input {:type (type i#) :val (display i#)}
                  :output {:type (type o#) :val (display o#)}
                  :result (equals i# o#)})
               (catch Exception ex#
                   {:iteraton x#
                    :input {:type (type i#) :val (display i#)}
                    :output (.Message ex#)
                    :result :failed})))
          (range times#))))

(deftest-times test-fressian-character-encoding gen/single-char-string)
(deftest-times test-fressian-scalars gen/scalar)
(deftest-times test-fressian-builtins gen/fressian-builtin)
(deftest-times test-fressian-int-packing gen/longs-near-powers-of-2)
(deftest-times test-fressian-names gen/symbolic)

(defn show-failures
  [testfn iters]
  (pprint (filter #(or (= :failed (:result %))
                       (= false (:result %)))
                  (testfn iters))))

(comment

  (show-failures test-fressian-character-encoding 1000)
  (show-failures test-fressian-scalars 10000)
  (show-failures test-fressian-builtins 100)
  (show-failures test-fressian-int-packing 1)
  (show-failures test-fressian-names 1000)
  
  
)

