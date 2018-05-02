using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

namespace SharpTAL.Tests.TalTests
{
	[TestFixture]
    public class TalRepeatTests
    {
        public static Dictionary<string, object> globals;

        [OneTimeSetUp]
        public void SetUpClass()
        {
        }

        [OneTimeTearDown]
        public void CleanupClass()
        {
        }

        [SetUp]
        public void SetUp()
        {
            globals = new Dictionary<string, object>();
            globals.Add("test", "testing"); ;
            globals.Add("one", new List<int>() { 1 });
            globals.Add("two", new List<string>() { "one", "two" });
            globals.Add("three", new List<object>() { 1, "Two", 3 });
            globals.Add("emptyList", new List<string>());
            List<int> bigList = new List<int>();
            for (int i = 1; i < 100; i++)
                bigList.Add(i);
            globals.Add("bigList", bigList);
            globals.Add("fourList", new List<string>() { "zero", "one", "two", "three" });
            globals.Add("nested", new List<Dictionary<string, IEnumerable>>()
            {
                {
                    new Dictionary<string, IEnumerable>()
                    {
                        { "title", "Image 1"}, { "catList", new List<int>() { 1, 2, 3} }
                    }
                },
                {
                    new Dictionary<string, IEnumerable>()
                    {
                        { "title", "Image 2"}, { "catList", new List<int>() { 5, 2, 3} }, { "selected", Constants.DefaultValue }
                    }
                },
                {
                    new Dictionary<string, IEnumerable>()
                    {
                        { "title", "Image 3"}, { "catList", new List<int>() { 8, 9, 1} }
                    }
                },
            });
            globals.Add("defList", new List<string>() { "Hello", Constants.DefaultValue, "World" });
            globals.Add("testString", "ABC"); ;
            globals.Add("testDict", new Dictionary<string, string>() { { "KeyA", "A" }, { "KeyB", "B" } }); ;
        }

        public static void RunTest(string template, string expected, string errMsg)
        {
			string actual = new Template(template).Render(globals);
			actual = actual.Replace("{", "{{").Replace("}", "}}");
			Assert.AreEqual(expected, actual, "{1} - {0}template: {2}{0}actual: {3}{0}expected: {4}",
				Environment.NewLine, errMsg, template, actual, expected);
		}

        [Test]
        public void TestInvalidPath()
        {
            Assert.Throws<CompileSourceException>(() => RunTest(@"<html><p tal:repeat=""entry wibble"">Hello</p></html>",
                                                                "<html></html>",
                                                                "Repeat of non-existant element failed"));
        }

        [Test]
        public void TestDefaultValue()
        {
            RunTest(@"<html><p tal:repeat=""entry default"">Default Only</p></html>",
                "<html><p>Default Only</p></html>",
                "Default did not keep existing structure intact");
        }

        [Test]
        public void TestStringRepeat()
        {
            RunTest(@"<html><p tal:omit-tag="""" tal:repeat=""letter test""><b tal:replace=""letter""></b></p></html>",
                "<html>testing</html>",
                "Itteration over string failed.");
        }

        [Test]
        public void TestEmptyList()
        {
            RunTest(@"<html><p tal:omit-tag="""" tal:repeat=""empty emptyList""><b tal:replace=""empty.Length"">Empty</b></p></html>",
                "<html></html>",
                "Empty list repeat failed.");
        }

        [Test]
        public void TestListRepeat()
        {
            RunTest(@"<html><p tal:repeat=""word two""><b tal:replace=""word""></b></p></html>",
                "<html><p>one</p><p>two</p></html>",
                "Itteration over list failed.");
        }

        [Test]
        public void TestTwoCmndsOneTagListRepeat()
        {
            RunTest(@"<html><p tal:repeat=""word two"" tal:content=""word""></p></html>",
                "<html><p>one</p><p>two</p></html>",
                "Itteration over list with both content and repeat on same element failed.");
        }

        [Test]
        public void TestAttributesInRepeat()
        {
            RunTest(
                @"<html><p tal:repeat=""words nested"" tal:content='words[""title""]' tal:attributes='selected words[""selected""]' selected=""selected""></p></html>",
                @"<html><p>Image 1</p><p selected=""selected"">Image 2</p><p>Image 3</p></html>",
                "Accessing attributes in repeat loop failed.");
        }

        [Test]
        public void TestDefaultInContentInRepeat()
        {
            RunTest(@"<html><p tal:repeat=""words defList"" tal:content=""words"">Default Word</p></html>",
                "<html><p>Hello</p><p>Default Word</p><p>World</p></html>",
                "Using default content in repeat loop failed.");
        }

        [Test]
        public void TestDefaultInReplaceInRepeat()
        {
            RunTest(@"<html><p tal:repeat=""words defList"" tal:replace=""words"">Default Word</p></html>",
                "<html>Hello<p>Default Word</p>World</html>",
                "Using default content in repeat loop failed.");
        }

        [Test]
        public void TestNestedRepeat()
        {
            RunTest(
                @"<html><p tal:repeat=""image nested""><h2 tal:content='image[""title""]'></h2><b tal:omit-tag="""" tal:repeat='category image[""catList""]'><i tal:content=""category""></i></b></p></html>",
                "<html><p><h2>Image 1</h2><i>1</i><i>2</i><i>3</i></p><p><h2>Image 2</h2><i>5</i><i>2</i><i>3</i></p><p><h2>Image 3</h2><i>8</i><i>9</i><i>1</i></p></html>",
                "Nested repeat did not create expected outcome.");
        }

        [Test]
        public void TestNestedRepeatClasses()
        {
            RunTest(
                @"<html><p class=""outerClass"" tal:repeat=""image nested""><div class=""innerClass"" tal:repeat='category image[""catList""]'><i tal:content=""category""></i></div></p></html>",
                @"<html><p class=""outerClass""><div class=""innerClass""><i>1</i></div><div class=""innerClass""><i>2</i></div><div class=""innerClass""><i>3</i></div></p><p class=""outerClass""><div class=""innerClass""><i>5</i></div><div class=""innerClass""><i>2</i></div><div class=""innerClass""><i>3</i></div></p><p class=""outerClass""><div class=""innerClass""><i>8</i></div><div class=""innerClass""><i>9</i></div><div class=""innerClass""><i>1</i></div></p></html>",
                "Nested repeat with classes did not create expected outcome.");
        }

        [Test]
        public void TestNestedRepeatScope()
        {
            Assert.Throws<>((CompileSourceException) => RunTest(
                @"<html><p tal:repeat=""image nested""><h2 tal:content='image[""title""]'></h2><b tal:omit-tag="""" tal:repeat='image image[""catList""]'><i tal:content=""image""></i></b></p></html>",
                @"<html><p><h2>Image 1</h2><i>1</i><i>2</i><i>3</i></p><p><h2>Image 2</h2><i>5</i><i>2</i><i>3</i></p><p><h2>Image 3</h2><i>8</i><i>9</i><i>1</i></p></html>",
                "Nested repeat did not create expected outcome."));
        }

        [Test]
        public void TestRepeatVarIndex()
        {
            string expectedResult = "<html>";
            for (int num = 0; num < 99; num++)
            {
                expectedResult += num.ToString();
            }
            expectedResult += "</html>";

            RunTest(
                @"<html><p tal:repeat=""val bigList"" tal:omit-tag=""""><b tal:replace='repeat[""val""].index'>Index</b></p></html>",
                expectedResult,
                "Repeat variable index failed.");
        }

        [Test]
        public void TestRepeatVarNumber()
        {
            RunTest(
                @"<html><p tal:repeat=""val bigList"" tal:omit-tag=""""><b tal:replace='repeat[""val""].number'>Index</b></p></html>",
                "<html>123456789101112131415161718192021222324252627282930313233343536373839404142434445464748495051525354555657585960616263646566676869707172737475767778798081828384858687888990919293949596979899</html>",
                "Repeat variable number failed.");
        }

        [Test]
        public void TestRepeatVarEvenOdd()
        {
            RunTest(
                @"<html><p tal:repeat=""val fourList""><i tal:replace=""val""></i> - <b tal:condition='repeat[""val""].odd'>Odd</b><b tal:condition='repeat[""val""].even'>Even</b></p></html>",
                "<html><p>zero - <b>Even</b></p><p>one - <b>Odd</b></p><p>two - <b>Even</b></p><p>three - <b>Odd</b></p></html>",
                "Repeat variables odd and even failed.");
        }

        [Test]
        public void TestRepeatVarStartEnd()
        {
            RunTest(
                @"<html><p tal:repeat=""val fourList""><b tal:condition='repeat[""val""].start'>Start</b><i tal:replace=""val""></i><b tal:condition='repeat[""val""].end'>End</b></p></html>",
                "<html><p><b>Start</b>zero</p><p>one</p><p>two</p><p>three<b>End</b></p></html>",
                "Repeat variables start and end failed.");
        }

        [Test]
        public void TestRepeatVarStartEndString()
        {
            RunTest(
                @"<html><p tal:repeat=""val testString""><b tal:condition='repeat[""val""].start'>Start</b><i tal:replace=""val""></i><b tal:condition='repeat[""val""].end'>End</b></p></html>",
                "<html><p><b>Start</b>A</p><p>B</p><p>C<b>End</b></p></html>",
                "Repeat variables start and end failed.");
        }

        [Test]
        public void TestRepeatVarStartEndDict()
        {
            RunTest(
                @"<html><p tal:repeat=""val testDict""><b tal:condition='repeat[""val""].start'>Start</b><i tal:replace='val.Value'></i><b tal:condition='repeat[""val""].end'>End</b></p></html>",
                "<html><p><b>Start</b>A</p><p>B<b>End</b></p></html>",
                "Repeat variables start and end failed.");
        }

        [Test]
        public void TestRepeatVarLength()
        {
            RunTest(
                @"<html><p tal:repeat=""val fourList""><b tal:condition='repeat[""val""].start'>Len: <i tal:replace='repeat[""val""].length'>length</i></b>Entry: <i tal:replace=""val""></i></p></html>",
                "<html><p><b>Len: 4</b>Entry: zero</p><p>Entry: one</p><p>Entry: two</p><p>Entry: three</p></html>",
                "Repeat variable length failed.");
        }

        [Test]
        public void TestRepeatVarLowerLetter()
        {
            RunTest(
                @"<html><p tal:repeat=""val fourList""><i tal:replace='repeat[""val""].letter'>a,b,c,etc</i>: <i tal:replace=""val""></i></p></html>",
                "<html><p>a: zero</p><p>b: one</p><p>c: two</p><p>d: three</p></html>",
                "Repeat variable letter failed.");
        }

        [Test]
        public void TestRepeatVarLowerLetterLarge()
        {
            RunTest(
                @"<html><p tal:repeat=""val bigList""><i tal:replace='repeat[""val""].letter'>a,b,c,etc</i>: <i tal:replace=""val""></i></p></html>",
                "<html><p>a: 1</p><p>b: 2</p><p>c: 3</p><p>d: 4</p><p>e: 5</p><p>f: 6</p><p>g: 7</p><p>h: 8</p><p>i: 9</p><p>j: 10</p><p>k: 11</p><p>l: 12</p><p>m: 13</p><p>n: 14</p><p>o: 15</p><p>p: 16</p><p>q: 17</p><p>r: 18</p><p>s: 19</p><p>t: 20</p><p>u: 21</p><p>v: 22</p><p>w: 23</p><p>x: 24</p><p>y: 25</p><p>z: 26</p><p>ba: 27</p><p>bb: 28</p><p>bc: 29</p><p>bd: 30</p><p>be: 31</p><p>bf: 32</p><p>bg: 33</p><p>bh: 34</p><p>bi: 35</p><p>bj: 36</p><p>bk: 37</p><p>bl: 38</p><p>bm: 39</p><p>bn: 40</p><p>bo: 41</p><p>bp: 42</p><p>bq: 43</p><p>br: 44</p><p>bs: 45</p><p>bt: 46</p><p>bu: 47</p><p>bv: 48</p><p>bw: 49</p><p>bx: 50</p><p>by: 51</p><p>bz: 52</p><p>ca: 53</p><p>cb: 54</p><p>cc: 55</p><p>cd: 56</p><p>ce: 57</p><p>cf: 58</p><p>cg: 59</p><p>ch: 60</p><p>ci: 61</p><p>cj: 62</p><p>ck: 63</p><p>cl: 64</p><p>cm: 65</p><p>cn: 66</p><p>co: 67</p><p>cp: 68</p><p>cq: 69</p><p>cr: 70</p><p>cs: 71</p><p>ct: 72</p><p>cu: 73</p><p>cv: 74</p><p>cw: 75</p><p>cx: 76</p><p>cy: 77</p><p>cz: 78</p><p>da: 79</p><p>db: 80</p><p>dc: 81</p><p>dd: 82</p><p>de: 83</p><p>df: 84</p><p>dg: 85</p><p>dh: 86</p><p>di: 87</p><p>dj: 88</p><p>dk: 89</p><p>dl: 90</p><p>dm: 91</p><p>dn: 92</p><p>do: 93</p><p>dp: 94</p><p>dq: 95</p><p>dr: 96</p><p>ds: 97</p><p>dt: 98</p><p>du: 99</p></html>",
                "Repeat variable letter failed on a large list.");
        }

        [Test]
        public void TestRepeatVarUpperLetter()
        {
            RunTest(
                @"<html><p tal:repeat=""val fourList""><i tal:replace='repeat[""val""].Letter'>A,B,C,etc</i>: <i tal:replace=""val""></i></p></html>",
                "<html><p>A: zero</p><p>B: one</p><p>C: two</p><p>D: three</p></html>",
                "Repeat variable Letter failed.");
        }

        [Test]
        public void TestRepeatVarLowerRoman()
        {
            RunTest(
                @"<html><p tal:repeat=""val bigList""><i tal:replace='repeat[""val""].roman'>i,ii,iii,etc</i>: <i tal:replace=""val""></i></p></html>",
                "<html><p>i: 1</p><p>ii: 2</p><p>iii: 3</p><p>iv: 4</p><p>v: 5</p><p>vi: 6</p><p>vii: 7</p><p>viii: 8</p><p>ix: 9</p><p>x: 10</p><p>xi: 11</p><p>xii: 12</p><p>xiii: 13</p><p>xiv: 14</p><p>xv: 15</p><p>xvi: 16</p><p>xvii: 17</p><p>xviii: 18</p><p>xix: 19</p><p>xx: 20</p><p>xxi: 21</p><p>xxii: 22</p><p>xxiii: 23</p><p>xxiv: 24</p><p>xxv: 25</p><p>xxvi: 26</p><p>xxvii: 27</p><p>xxviii: 28</p><p>xxix: 29</p><p>xxx: 30</p><p>xxxi: 31</p><p>xxxii: 32</p><p>xxxiii: 33</p><p>xxxiv: 34</p><p>xxxv: 35</p><p>xxxvi: 36</p><p>xxxvii: 37</p><p>xxxviii: 38</p><p>xxxix: 39</p><p>xl: 40</p><p>xli: 41</p><p>xlii: 42</p><p>xliii: 43</p><p>xliv: 44</p><p>xlv: 45</p><p>xlvi: 46</p><p>xlvii: 47</p><p>xlviii: 48</p><p>xlix: 49</p><p>l: 50</p><p>li: 51</p><p>lii: 52</p><p>liii: 53</p><p>liv: 54</p><p>lv: 55</p><p>lvi: 56</p><p>lvii: 57</p><p>lviii: 58</p><p>lix: 59</p><p>lx: 60</p><p>lxi: 61</p><p>lxii: 62</p><p>lxiii: 63</p><p>lxiv: 64</p><p>lxv: 65</p><p>lxvi: 66</p><p>lxvii: 67</p><p>lxviii: 68</p><p>lxix: 69</p><p>lxx: 70</p><p>lxxi: 71</p><p>lxxii: 72</p><p>lxxiii: 73</p><p>lxxiv: 74</p><p>lxxv: 75</p><p>lxxvi: 76</p><p>lxxvii: 77</p><p>lxxviii: 78</p><p>lxxix: 79</p><p>lxxx: 80</p><p>lxxxi: 81</p><p>lxxxii: 82</p><p>lxxxiii: 83</p><p>lxxxiv: 84</p><p>lxxxv: 85</p><p>lxxxvi: 86</p><p>lxxxvii: 87</p><p>lxxxviii: 88</p><p>lxxxix: 89</p><p>xc: 90</p><p>xci: 91</p><p>xcii: 92</p><p>xciii: 93</p><p>xciv: 94</p><p>xcv: 95</p><p>xcvi: 96</p><p>xcvii: 97</p><p>xcviii: 98</p><p>xcix: 99</p></html>",
                "Repeat variable roman failed.");
        }

        [Test]
        public void TestRepeatVarUpperRoman()
        {
            RunTest(
                @"<html><p tal:repeat=""val bigList""><i tal:replace='repeat[""val""].Roman'>I,II,III,etc</i>: <i tal:replace=""val""></i></p></html>",
                "<html><p>I: 1</p><p>II: 2</p><p>III: 3</p><p>IV: 4</p><p>V: 5</p><p>VI: 6</p><p>VII: 7</p><p>VIII: 8</p><p>IX: 9</p><p>X: 10</p><p>XI: 11</p><p>XII: 12</p><p>XIII: 13</p><p>XIV: 14</p><p>XV: 15</p><p>XVI: 16</p><p>XVII: 17</p><p>XVIII: 18</p><p>XIX: 19</p><p>XX: 20</p><p>XXI: 21</p><p>XXII: 22</p><p>XXIII: 23</p><p>XXIV: 24</p><p>XXV: 25</p><p>XXVI: 26</p><p>XXVII: 27</p><p>XXVIII: 28</p><p>XXIX: 29</p><p>XXX: 30</p><p>XXXI: 31</p><p>XXXII: 32</p><p>XXXIII: 33</p><p>XXXIV: 34</p><p>XXXV: 35</p><p>XXXVI: 36</p><p>XXXVII: 37</p><p>XXXVIII: 38</p><p>XXXIX: 39</p><p>XL: 40</p><p>XLI: 41</p><p>XLII: 42</p><p>XLIII: 43</p><p>XLIV: 44</p><p>XLV: 45</p><p>XLVI: 46</p><p>XLVII: 47</p><p>XLVIII: 48</p><p>XLIX: 49</p><p>L: 50</p><p>LI: 51</p><p>LII: 52</p><p>LIII: 53</p><p>LIV: 54</p><p>LV: 55</p><p>LVI: 56</p><p>LVII: 57</p><p>LVIII: 58</p><p>LIX: 59</p><p>LX: 60</p><p>LXI: 61</p><p>LXII: 62</p><p>LXIII: 63</p><p>LXIV: 64</p><p>LXV: 65</p><p>LXVI: 66</p><p>LXVII: 67</p><p>LXVIII: 68</p><p>LXIX: 69</p><p>LXX: 70</p><p>LXXI: 71</p><p>LXXII: 72</p><p>LXXIII: 73</p><p>LXXIV: 74</p><p>LXXV: 75</p><p>LXXVI: 76</p><p>LXXVII: 77</p><p>LXXVIII: 78</p><p>LXXIX: 79</p><p>LXXX: 80</p><p>LXXXI: 81</p><p>LXXXII: 82</p><p>LXXXIII: 83</p><p>LXXXIV: 84</p><p>LXXXV: 85</p><p>LXXXVI: 86</p><p>LXXXVII: 87</p><p>LXXXVIII: 88</p><p>LXXXIX: 89</p><p>XC: 90</p><p>XCI: 91</p><p>XCII: 92</p><p>XCIII: 93</p><p>XCIV: 94</p><p>XCV: 95</p><p>XCVI: 96</p><p>XCVII: 97</p><p>XCVIII: 98</p><p>XCIX: 99</p></html>",
                "Repeat variable Roman failed.");
        }
    }
}
