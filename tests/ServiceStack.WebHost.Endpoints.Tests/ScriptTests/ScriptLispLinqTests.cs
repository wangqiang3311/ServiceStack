using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.ScriptTests
{
    public class ScriptLispLinqTests
    {
        private static ScriptContext CreateContext()
        {
            var context = new ScriptContext
            {
                ScriptLanguages = { ScriptLisp.Language },
                ScriptMethods = {
                    new ProtectedScripts(),
                },
                Args =
                {
                    [ScriptConstants.DefaultDateFormat] = "yyyy/MM/dd",
                    ["products"] = QueryData.Products,
                    ["customers"] = QueryData.Customers,
                    ["comparer"] = new CaseInsensitiveComparer(),
                    ["anagramComparer"] = new QueryFilterTests.AnagramEqualityComparer(),
                }
            };
            Lisp.Set("products-list", Lisp.ToCons(QueryData.Products));
            Lisp.Set("customers-list", Lisp.ToCons(QueryData.Customers));
            return context.Init();
        }

        [SetUp]
        public void Setup() => context = CreateContext();
        private ScriptContext context;

        string render(string lisp) => context.RenderLisp(lisp).NormalizeNewLines();

        void print(string lisp) => render(lisp).Print();
        object eval(string lisp) => context.EvaluateLisp($"(return {lisp})");

        [Test]
        public void Linq01() 
        {
            Assert.That(render(@"
(defn linq01 ()
    (setq numbers '(5 4 1 3 9 8 6 7 2 0))
    (let ((low-numbers (filter (fn (%) (< % 5)) numbers)))
        (println ""Numbers < 5:"")
        (dolist (n low-numbers)
            (println n))))
(linq01)"), 
                
                Is.EqualTo(@"
Numbers < 5:
4
1
3
2
0
".NormalizeNewLines()));
        }

        [Test]
        public void Linq02()
        {
            Assert.That(render(@"
(defn linq02 ()
    (let ( (sold-out-products 
               (filter (fn (p) (= 0 (.UnitsInStock p))) products-list)) )
        (println ""Sold out products:"")
        (doseq (p sold-out-products)
            (println (.ProductName p) "" is sold out"") )
    ))
(linq02)"), 
                
                Is.EqualTo(@"
Sold out products:
Chef Anton's Gumbo Mix is sold out
Alice Mutton is sold out
Thüringer Rostbratwurst is sold out
Gorgonzola Telino is sold out
Perth Pasties is sold out
".NormalizeNewLines()));
        }

        [Test]
        public void Linq03()
        {
            Assert.That(render(@"
(defn linq03 ()
  (let ( (expensive-in-stock-products
            (filter (fn (p) 
                (and
                     (> (.UnitsInStock p) 0)
                     (> (.UnitPrice p) 3)) )
             products-list)
         ))
    (println ""In-stock products that cost more than 3.00:"")
    (doseq (p expensive-in-stock-products)
      (println (.ProductName p) "" is in stock and costs more than 3.00""))))

(linq03)"), 
                
                Does.StartWith(@"
In-stock products that cost more than 3.00:
Chai is in stock and costs more than 3.00
Chang is in stock and costs more than 3.00
Aniseed Syrup is in stock and costs more than 3.00
Chef Anton's Cajun Seasoning is in stock and costs more than 3.00
Grandma's Boysenberry Spread is in stock and costs more than 3.00
".NormalizeNewLines()));
        }

        [Test]
        public void Linq04()
        {
            Assert.That(render(@"
(defn linq04 ()
    (let ( (wa-customers (filter (fn (x) (= (.Region x) ""WA"")) customers-list)) )
        (println ""Customers from Washington and their orders:"")
        (doseq (c wa-customers)
            (println ""Customer "" (.CustomerId c) "": "" (.CompanyName c) "": "")
            (doseq (o (.Orders c))
                (println ""    Order "" (.OrderId o) "": "" (.OrderDate o)) )
        )))
(linq04)"), 
                
                Does.StartWith(@"
Customers from Washington and their orders:
Customer LAZYK: Lazy K Kountry Store: 
    Order 10482: 3/21/1997 12:00:00 AM
    Order 10545: 5/22/1997 12:00:00 AM
Customer TRAIH: Trail's Head Gourmet Provisioners: 
    Order 10574: 6/19/1997 12:00:00 AM
    Order 10577: 6/23/1997 12:00:00 AM
    Order 10822: 1/8/1998 12:00:00 AM
".NormalizeNewLines()));
        }

        [Test]
        public void Linq05()
        {
            Assert.That(render(@"
(defn linq05 ()
(let ( (digits '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""))
       (i 0) (short-digits) )
    (setq short-digits (filter (fn (digit) (if (> (1- (incf i)) (/count digit)) digit)) digits))
    (println ""Short digits:"")
    (doseq (d short-digits)
      (println ""The word "" d "" is shorter than its value""))))
(linq05)"), 
                
                Does.StartWith(@"
Short digits:
The word five is shorter than its value
The word six is shorter than its value
The word seven is shorter than its value
The word eight is shorter than its value
The word nine is shorter than its value
".NormalizeNewLines()));
        }

        [Test]
        public void Linq06()
        {
            Assert.That(render(@"
(defn linq06 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0)) (nums-plus-one) )
    (setq nums-plus-one (map inc numbers))
    (println ""Numbers + 1:"")
        (doseq (n nums-plus-one) (println n))))
(linq06)"), 
                
                Does.StartWith(@"
Numbers + 1:
6
5
2
4
10
9
7
8
3
1
".NormalizeNewLines()));
        }

        [Test]
        public void Linq07()
        {
            Assert.That(render(@"
(defn linq07 ()
  (let ( (product-names (map (fn (x) (.ProductName x)) products-list)) )
    (println ""Product Names:"")
    (doseq (x product-names) (println x))))
(linq07)"), 
                
                Does.StartWith(@"
Product Names:
Chai
Chang
Aniseed Syrup
Chef Anton's Cajun Seasoning
Chef Anton's Gumbo Mix
".NormalizeNewLines()));
        }

        [Test]
        public void Linq08()
        {
            Assert.That(render(@"
(defn linq08 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
         (strings '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine"")) 
         (text-nums) )
      (setq text-nums (map (fn (n) (nth strings n)) numbers))
      (println ""Number strings:"")
      (doseq (n text-nums) (println n))
  ))
(linq08)"), 
                
                Does.StartWith(@"
Number strings:
five
four
one
three
nine
eight
six
seven
two
zero
".NormalizeNewLines()));
        }

        [Test]
        public void Linq09()
        {
            Assert.That(render(@"
(defn linq09 ()
  (let ( (words '(""aPPLE"" ""BlUeBeRrY"" ""cHeRry""))
         (upper-lower-words) )
    (setq upper-lower-words
        (map (fn (w) `( (lower ,(lower-case w)) (upper ,(upper-case w)) )) words) )
    (doseq (ul upper-lower-words)
        (println ""Uppercase: "" (assoc-value 'upper ul) "", Lowercase: "" (assoc-value 'lower ul)))
  ))
(linq09)"), 
                
                Does.StartWith(@"
Uppercase: APPLE, Lowercase: apple
Uppercase: BLUEBERRY, Lowercase: blueberry
Uppercase: CHERRY, Lowercase: cherry
".NormalizeNewLines()));
        }

        [Test]
        public void Linq10()
        {
            Assert.That(render(@"
(defn linq10 ()
  (let ( (numbers '(5 4 1 3 9 8 6 7 2 0))
         (strings '(""zero"" ""one"" ""two"" ""three"" ""four"" ""five"" ""six"" ""seven"" ""eight"" ""nine""))
         (digit-odd-evens) )
      (setq digit-odd-evens 
          (map (fn(n) `( (digit ,(nth strings n)) (even ,(even? n)) ) ) numbers))
      (doseq (d digit-odd-evens)
          (println ""The digit "" (assoc-value 'digit d) "" is "" (if (assoc-value 'even d) ""even"" ""odd"")))
  ))
(linq10)"), 
                
                Does.StartWith(@"
The digit five is odd
The digit four is even
The digit one is odd
The digit three is odd
The digit nine is odd
The digit eight is even
The digit six is even
The digit seven is odd
The digit two is even
The digit zero is even
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (x) {
                    :ProductName (.ProductName x)
                    :Category    (.Category x)
                    :Price       (.UnitPrice x) 
                 }) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
        (println (:ProductName p) "" is in the category "" (:Category p) "" and costs "" (:Price p)) )
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }

        [Test]
        public void Linq11_expanded()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (x) (new-map
                    (list ""ProductName"" (.ProductName x))
                    (list ""Category""    (.Category x))
                    (list ""Price""       (.UnitPrice x)) 
                )) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
      (println (:ProductName p) "" is in the category "" (:Category p) "" and costs "" (:Price p)))
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }

        [Test]
        public void test()
        {
//            print("(fn (x) (.ProductName x))");
//            print(@"(fn (x) (new-map (list ""ProductName"" (.ProductName x)) ))");
        }

        [Test]
        public void Linq11_classic_lisp()
        {
            Assert.That(render(@"
(defn linq11 ()
  (let ( (product-infos
            (map (fn (p) `(
                    (ProductName ,(.ProductName p))
                    (Category    ,(.Category p))
                    (Price       ,(.UnitPrice p)) 
                )) 
            products-list)) )
    (println ""Product Info:"")
    (doseq (p product-infos)
      (println (assoc-value 'ProductName p) "" is in the category "" (assoc-value 'Category p) 
               "" and costs "" (assoc-value 'Price p)))
  ))
(linq11)"), 
                
                Does.StartWith(@"
Product Info:
Chai is in the category Beverages and costs 18
Chang is in the category Beverages and costs 19
Aniseed Syrup is in the category Condiments and costs 10
Chef Anton's Cajun Seasoning is in the category Condiments and costs 22
Chef Anton's Gumbo Mix is in the category Condiments and costs 21.35
".NormalizeNewLines()));
        }
        
    }
}