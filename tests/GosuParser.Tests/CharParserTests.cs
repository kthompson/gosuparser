﻿using System;
using System.Collections.Generic;
using System.Linq;
using GosuParser.Input;
using Xunit;
using static GosuParser.CharacterParsers;

namespace GosuParser.Tests
{
    public class CharParserTests
    {
        [Fact]
        public void CharParserParsesSuccessfully()
        {
            var parser = Char('A');

            AssertSuccess(parser, "ABC", "BC", c => Assert.Equal('A', c));
            AssertFailure(parser, "ZBC", "Line:1 Col:1 Error parsing 'A'\nZBC\n^ Unexpected 'Z'");
        }

        [Fact]
        public void AndThenCombineParsers()
        {
            var parseA = Char('A');
            var parseB = Char('B');
            var parseAThenB = parseA.AndThen(parseB);

            AssertSuccess(parseAThenB, "ABC", "C", tuple =>
            {
                Assert.Equal('A', tuple.Item1);
                Assert.Equal('B', tuple.Item2);
            });

            AssertFailure(parseAThenB, "ZBC", "Line:1 Col:1 Error parsing 'A'\nZBC\n^ Unexpected 'Z'");
            AssertFailure(parseAThenB, "AZC", "Line:1 Col:2 Error parsing 'B'\nAZC\n ^ Unexpected 'Z'");
        }

        [Fact]
        public void OrElseCombineParsers()
        {
            var parseA = Char('A');
            var parseB = Char('B');
            var parseAOrElseB = parseA.OrElse(parseB);

            AssertSuccess(parseAOrElseB, "AZZ", "ZZ", value => Assert.Equal('A', value));
            AssertSuccess(parseAOrElseB, "BZZ", "ZZ", value => Assert.Equal('B', value));
            AssertFailure(parseAOrElseB, "CZZ", "Line:1 Col:1 Error parsing 'B'\nCZZ\n^ Unexpected 'C'");
        }

        [Fact]
        public void AndThenOrElseCombineParsers()
        {
            var parseA = Char('A');
            var parseB = Char('B');
            var parseC = Char('C');
            var bOrElseC = parseB.OrElse(parseC);
            var aAndThenBorC = parseA.AndThen(bOrElseC);

            AssertSuccess(aAndThenBorC, "ABZ", "Z", tuple =>
            {
                Assert.Equal('A', tuple.Item1);
                Assert.Equal('B', tuple.Item2);
            });

            AssertSuccess(aAndThenBorC, "ACZ", "Z", tuple =>
            {
                Assert.Equal('A', tuple.Item1);
                Assert.Equal('C', tuple.Item2);
            });

            AssertFailure(aAndThenBorC, "QBZ", "Line:1 Col:1 Error parsing 'A'\nQBZ\n^ Unexpected 'Q'");
            AssertFailure(aAndThenBorC, "AQZ", "Line:1 Col:2 Error parsing 'C'\nAQZ\n ^ Unexpected 'Q'");
        }

        [Fact]
        public void ParseLowercase()
        {
            var parseLowercase = AnyOf("abcdefghijklmnopqrstuvwxyz");

            AssertSuccess(parseLowercase, "aBC", "BC", c => Assert.Equal('a', c));
            AssertFailure(parseLowercase, "ABC", "Line:1 Col:1 Error parsing any of [abcdefghijklmnopqrstuvwxyz]\nABC\n^ Unexpected 'A'");

            var parseDigit = AnyOf("0123456789");

            AssertSuccess(parseDigit, "1ABC", "ABC", c => Assert.Equal('1', c));
            AssertSuccess(parseDigit, "9ABC", "ABC", c => Assert.Equal('9', c));
            AssertFailure(parseDigit, "|ABC", "Line:1 Col:1 Error parsing any of [0123456789]\n|ABC\n^ Unexpected '|'");
        }

        [Fact]
        public void ParseThreeDigits()
        {
            var parseDigit = AnyOf("0123456789");

            var parseThreeDigitsAsStr =
                parseDigit
                    .AndThen(parseDigit)
                    .AndThen(parseDigit)
                    .Select(x => new string(new char[] { x.Item1, x.Item2, x.Item3 }));

            AssertSuccess(parseThreeDigitsAsStr, "123A", "A", s => Assert.Equal("123", s));

            var parseThreeDigitsAsInt = parseThreeDigitsAsStr.Select(int.Parse);

            AssertSuccess(parseThreeDigitsAsInt, "123A", "A", i => Assert.Equal(123, i));
        }

        [Fact]
        public void SequenceTest()
        {
            var parsers = new List<Parser<char, char>>()
            {
                Char('A'),
                Char('B'),
                Char('C')
            };

            var combined = parsers.ToSequence();

            AssertSuccess(combined, "ABCD", "D", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('A', c),
                    c => Assert.Equal('B', c),
                    c => Assert.Equal('C', c));
            });
        }

        [Fact]
        public void StringParserTest()
        {
            var parseABC = String("ABC");

            AssertSuccess(parseABC, "ABCD", "D", str => Assert.Equal("ABC", str));
            AssertFailure(parseABC, "A|CDE", "Line:1 Col:2 Error parsing ABC\nA|CDE\n ^ Unexpected '|'");
            AssertFailure(parseABC, "AB|DE", "Line:1 Col:3 Error parsing ABC\nAB|DE\n  ^ Unexpected '|'");
        }

        [Fact]
        public void ManyCharParserTest()
        {
            var manyA = Char('A').ZeroOrMore();

            AssertSuccess(manyA, "ABCD", "BCD", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('A', c));
            });

            AssertSuccess(manyA, "AACD", "CD", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('A', c),
                    c => Assert.Equal('A', c));
            });

            AssertSuccess(manyA, "AAAD", "D", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('A', c),
                    c => Assert.Equal('A', c),
                    c => Assert.Equal('A', c));
            });

            AssertSuccess(manyA, "|BCD", "|BCD", Assert.Empty);
        }

        [Fact]
        public void ManyStringParserTest()
        {
            var manyA = String("AB").ZeroOrMore();

            AssertSuccess(manyA, "ABCD", "CD", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal("AB", c));
            });

            AssertSuccess(manyA, "ABABCD", "CD", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal("AB", c),
                    c => Assert.Equal("AB", c));
            });

            AssertSuccess(manyA, "ZCD", "ZCD", Assert.Empty);
            AssertSuccess(manyA, "AZCD", "AZCD", Assert.Empty);
        }

        [Fact]
        public void WhitespaceParserTest()
        {
            var manyA = Spaces;

            AssertSuccess(manyA, "ABC", "ABC", Assert.Empty);

            AssertSuccess(manyA, " ABC", "ABC", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal(' ', c));
            });

            AssertSuccess(manyA, "\tABC", "ABC", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('\t', c));
            });
        }

        [Fact]
        public void Many1Test()
        {
            var digit = AnyOf("0123456789");
            var digits = digit.OneOrMore();

            AssertSuccess(digits, "1ABC", "ABC", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('1', c));
            });

            AssertSuccess(digits, "12BC", "BC", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c));
            });

            AssertSuccess(digits, "123C", "C", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c),
                    c => Assert.Equal('3', c));
            });

            AssertSuccess(digits, "1234", "", list =>
            {
                Assert.Collection(list,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c),
                    c => Assert.Equal('3', c),
                    c => Assert.Equal('4', c));
            });

            AssertFailure(digits, "ABC", "Line:1 Col:1 Error parsing any of [0123456789]\nABC\n^ Unexpected 'A'");
        }

        [Fact]
        public void IntParserTest()
        {
            var pint = IntParser;

            AssertSuccess(pint, "1ABC", "ABC", i => Assert.Equal(1, i));
            AssertSuccess(pint, "12BC", "BC", i => Assert.Equal(12, i));
            AssertSuccess(pint, "123C", "C", i => Assert.Equal(123, i));
            AssertSuccess(pint, "1234", "", i => Assert.Equal(1234, i));

            AssertFailure(pint, "ABC", "Line:1 Col:1 Error parsing digit\nABC\n^ Unexpected 'A'");

            AssertSuccess(pint, "-123C", "C", i => Assert.Equal(-123, i));
        }

        [Fact]
        public void OptionalTest()
        {
            var digit = AnyOf("0123456789");
            var digitThenSemicolon = digit.AndThen(Char(';').Optional());

            AssertSuccess(digitThenSemicolon, "1;", "", item =>
            {
                Assert.Equal('1', item.Item1);
                Assert.True(item.Item2.HasValue);
                Assert.Equal(';', item.Item2.Value);
            });

            AssertSuccess(digitThenSemicolon, "1", "", item =>
            {
                Assert.Equal('1', item.Item1);
                Assert.False(item.Item2.HasValue);
            });
        }

        [Fact]
        public void TakeLeftTest()
        {
            var digit = AnyOf("0123456789");
            var digitThenSemicolon = digit.TakeLeft(Char(';').Optional());

            AssertSuccess(digitThenSemicolon, "1;", "", c => Assert.Equal('1', c));
            AssertSuccess(digitThenSemicolon, "1", "", c => Assert.Equal('1', c));
        }

        [Fact]
        public void TakeLeftTest2()
        {
            var ab = String("AB");
            var cd = String("CD");

            var abCd = ab.TakeLeft(Spaces).AndThen(cd);

            AssertSuccess(abCd, "AB \t\nCD", "", tuple =>
            {
                Assert.Equal("AB", tuple.Item1);
                Assert.Equal("CD", tuple.Item2);
            });
        }

        [Fact]
        public void BetweenTest()
        {
            var quote = Char('"');
            var quotedInt = IntParser.Between(quote, quote);

            AssertSuccess(quotedInt, "\"1234\"", "", i => Assert.Equal(1234, i));
            AssertFailure(quotedInt, "1234", "Line:1 Col:1 Error parsing '\"'\n1234\n^ Unexpected '1'");
        }

        [Fact]
        public void SepBy1Test()
        {
            var comma = Char(',');
            var digit = AnyOf("0123456789");

            var oneOrMoreDigitList = digit.SepBy1(comma);

            AssertSuccess(oneOrMoreDigitList, "1;", ";", coll =>
            {
                Assert.Collection(coll, c => Assert.Equal('1', c));
            });

            AssertSuccess(oneOrMoreDigitList, "1,2;", ";", coll =>
            {
                Assert.Collection(coll,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c));
            });

            AssertSuccess(oneOrMoreDigitList, "1,2,3;", ";", coll =>
            {
                Assert.Collection(coll,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c),
                    c => Assert.Equal('3', c));
            });

            AssertFailure(oneOrMoreDigitList, "Z;", "Line:1 Col:1 Error parsing any of [0123456789]\nZ;\n^ Unexpected 'Z'");
        }

        [Fact]
        public void SepByTest()
        {
            var comma = Char(',');
            var digit = AnyOf("0123456789");
            var zeroOrMoreDigitList = digit.SepBy(comma);

            AssertSuccess(zeroOrMoreDigitList, "1;", ";", coll =>
            {
                Assert.Collection(coll, c => Assert.Equal('1', c));
            });

            AssertSuccess(zeroOrMoreDigitList, "1,2;", ";", coll =>
            {
                Assert.Collection(coll,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c));
            });

            AssertSuccess(zeroOrMoreDigitList, "1,2,3;", ";", coll =>
            {
                Assert.Collection(coll,
                    c => Assert.Equal('1', c),
                    c => Assert.Equal('2', c),
                    c => Assert.Equal('3', c));
            });

            AssertSuccess(zeroOrMoreDigitList, "Z;", "Z;", Assert.Empty);
        }

        [Fact]
        public void TestDigitWithLabel()
        {
            var parseDigit = AnyOf("0123456789").WithLabel("digit");

            var result = parseDigit.Run("|ABC").ToString();

            Assert.Equal("Line:1 Col:1 Error parsing digit\n|ABC\n^ Unexpected '|'", result);
        }

        private class PositionReader : Reader<char>
        {
            public PositionReader(string currentLine, int line, int column)
            {
                this.Position = new SimplePosition(currentLine, line, column);
            }

            public override char First => '0';
            public override Reader<char> Rest => null;
            public override bool AtEnd => true;
            public override Position Position { get; }
        }

        [Fact]
        public void TestFailureResult()
        {
            var failure = new Parser<char, int>.Failure("", "unexpected |", new PositionReader("123 ab|cd", 1, 6));

            var text = failure.ToString();
            Assert.Equal("Line:1 Col:6 Error parsing:\n123 ab|cd\n     ^ unexpected |", text);
        }

        [Fact]
        public void TestFailureResult2()
        {
            var failure = new Parser<char, int>.Failure("taco", "unexpected |", new PositionReader("123 ab|cd", 1, 6));

            var text = failure.ToString();
            Assert.Equal("Line:1 Col:6 Error parsing taco\n123 ab|cd\n     ^ unexpected |", text);
        }

        [Fact]
        public void BasicStringTest()
        {
            var jstring = from chars in Satisfy(c => c != '"' && c != '\\').Many()
                          select new string(chars.ToArray());

            AssertSuccess(jstring, @"hello", res => Assert.Equal("hello", res));
        }

        [Fact]
        public void JsonStringTest()
        {
            var jstring = from chars in Satisfy(c => c != '"' && c != '\\').Many().Between(Char('"'), Char('"'))
                          select new string(chars.ToArray());

            AssertSuccess(jstring, @"""hello""", res => Assert.Equal("hello", res));
        }

        [Fact]
        public void JsonKeyPairTest()
        {
            var jstring = from chars in Satisfy(c => c != '"' && c != '\\').Many().Between(Char('"'), Char('"'))
                          select new string(chars.ToArray());
            var key = jstring.WithLabel("key");
            jstring = jstring.WithLabel("value");
            var keyPair = key.TakeLeft(String(":")).AndThen(jstring).WithLabel("KeyPair");

            AssertSuccess(keyPair, "\"x\":\"5\"", result =>
            {
                Assert.NotNull(result);

                Assert.Equal("x", result.Item1);
                Assert.Equal("5", result.Item2);
            });
        }

        [Fact]
        public void JsonKeyPairsTest()
        {
            var jstring = from chars in Satisfy(c => c != '"' && c != '\\').Many().Between(Char('"'), Char('"'))
                          select new string(chars.ToArray());
            var key = jstring.WithLabel("key");
            jstring = jstring.WithLabel("value");
            var keyPair = key.TakeLeft(String(":")).AndThen(jstring).WithLabel("KeyPair");
            var keyPairs = keyPair.SepBy(String(","));

            AssertSuccess(keyPairs, "\"x\":\"5\",\"a\":\"hello\"", result =>
            {
                Assert.NotNull(result);
                Assert.Collection(result, tuple =>
                {
                    Assert.Equal("x", tuple.Item1);
                    Assert.Equal("5", tuple.Item2);
                }, tuple =>
                {
                    Assert.Equal("a", tuple.Item1);
                    Assert.Equal("hello", tuple.Item2);
                });
            });
        }

        [Fact]
        public void JsonTest()
        {
            Action<Parser<char, object>> assignment;
            Parser<char, object> jvalue = CreateParserForwardedToRef(out assignment);

            var jstring = from chars in Satisfy(c => c != '"' && c != '\\').Many().Between(Char('"'), Char('"'))
                          select new string(chars.ToArray());
            var key = jstring.WithLabel("key");
            jstring = jstring.WithLabel("string");
            var keyPair = key.TakeLeft(String(":")).AndThen(jvalue).WithLabel("KeyPair");
            var keyPairs = keyPair.SepBy(String(","));
            var jobject = from pairs in keyPairs.Between(Char('{'), Char('}'))
                          select (object)pairs.ToDictionary(t => t.Item1, t => t.Item2);

            var jarray = from value in jvalue.SepBy(Char(',')).Between(Char('['), Char(']'))
                         select (object)value.ToArray();

            var jnumber = from i in IntParser
                          select (object)i;
            assignment(new[]
            {
                jstring.Select(x => (object) x),
                jnumber,
                jobject,
                jarray,
                String("true").Select(x => (object) true),
                String("false").Select(x => (object) false),
                String("null").Select(x => (object) null)
            }.Choice().WithLabel("value"));

            AssertSuccess(jobject, "{\"x\":5,\"a\":\"hello\"}", result =>
            {
                Assert.NotNull(result);
                var o = Assert.IsType<Dictionary<string, object>>(result);
                Assert.Equal(2, o.Count);
            });
        }

        private static void AssertFailure<T>(Parser<char, T> parseAOrElseB, string input, string failureText)
        {
            var failure2 = Assert.IsType<Parser<char, T>.Failure>(parseAOrElseB.Run(input));
            Assert.False(failure2.IsSuccess);
            Assert.Equal(failureText, failure2.ToString());
        }

        private static void AssertSuccess<T>(Parser<char, T> parser, string input, Action<T> expectation)
        {
            AssertSuccess(parser, input, "", expectation);
        }

        private static void AssertSuccess<T>(Parser<char, T> parser, string input, string remainingInput, Action<T> expectation)
        {
            var run = parser.Run(input);
            var success = Assert.IsType<Parser<char, T>.Success>(run);
            Assert.True(success.IsSuccess);
            Assert.Equal(remainingInput, success.RemainingInput.GetRemainingInput());

            expectation(success.Value);
        }
    }
}