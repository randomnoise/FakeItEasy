namespace FakeItEasy.Analyzer.CSharp.Tests
{
    using System.Collections.Generic;
    using FakeItEasy.Analyzer.Tests.Helpers;
    using FakeItEasy.Tests;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    public class RepeatedAssertionAnalyzerTests : CodeFixVerifier
    {
        private const string CodeTemplate = @"using FakeItEasy;
namespace TheNamespace
{{
    class TheClass
    {{
        void TheTest()
        {{
            var fake = A.Fake<IFoo>();
            A.CallTo(() => fake.Method()).{0};
        }}
    }}

    interface IFoo
    {{
        void Method();
    }}
}}";

        [Theory]
        [MemberData(nameof(AssertionsThatDoNotUseRepeated))]
        public void Diagnostic_Should_Not_Be_Triggered_For_Assertion_That_Does_Not_Use_Repeated(string assertion)
        {
            var code = string.Format(CodeTemplate, assertion);
            this.VerifyCSharpDiagnostic(code);
        }

        [Theory]
        [MemberData(nameof(AssertionsThatUseRepeated))]
        public void Diagnostic_Should_Be_Triggered_For_MustHaveHappened_With_Repeated_Argument(string assertion)
        {
            var code = string.Format(CodeTemplate, assertion);
            this.VerifyCSharpDiagnostic(
                 code,
                 new DiagnosticResult
                 {
                     Id = "FakeItEasy0006",
                     Message = "The MustHaveHappened(Repeated) assertion is being retired, and will be deprecated in FakeItEasy version 5.0.0 and removed in version 6.0.0.",
                     Severity = DiagnosticSeverity.Warning,
                     Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 43) }
                 });
        }

        [Theory]
        [MemberData(nameof(AssertionsAndTheirReplacements))]
        public void ChangeAssertion_CodeFix_Should_Replace_Assertion_With_Repeatedless_Assertion(string assertion, string fixedAssertion)
        {
            string code = string.Format(CodeTemplate, assertion);
            string fixedCode = string.Format(CodeTemplate, fixedAssertion);

            this.VerifyCSharpFix(code, fixedCode, codeFixIndex: 0);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RepeatedAssertionAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new RepeatedAssertionCodeFixProvider();
        }

        private static IEnumerable<object[]> AssertionsThatDoNotUseRepeated() =>
            TestCases.FromObject(
                "MustHaveHappened()",
                "MustNotHaveHappened()",
                "MustHaveHappenedOnceExactly()",
                "MustHaveHappenedOnceOrMore()",
                "MustHaveHappenedOnceOrLess()",
                "MustHaveHappenedTwiceExactly()",
                "MustHaveHappenedTwiceOrMore()",
                "MustHaveHappenedTwiceOrLess()",
                "MustHaveHappened(3, Times.Exactly)",
                "MustHaveHappened(3, Times.OrMore)",
                "MustHaveHappened(3, Times.OrLess)",
                "MustHaveHappenedANumberOfTimesMatching(n => n % 2 == 0)");

        private static IEnumerable<object[]> AssertionsThatUseRepeated() =>
            TestCases.FromObject(
                "MustHaveHappened(Repeated.Never)",
                "MustHaveHappened(Repeated.Exactly.Once)",
                "MustHaveHappened(Repeated.AtLeast.Twice)",
                "MustHaveHappened(Repeated.NoMoreThan.Times(3))",
                "MustHaveHappened(Repeated.Like(n => n < 3 || n > 19))");

        private static IEnumerable<object[]> AssertionsAndTheirReplacements() =>
            new[]
            {
                new[] { "MustHaveHappened(Repeated.Never)", "MustNotHaveHappened()" },
                new[] { "MustHaveHappened(Repeated.Exactly.Once)", "MustHaveHappenedOnceExactly()" },
                new[] { "MustHaveHappened(Repeated.AtLeast.Once)", "MustHaveHappenedOnceOrMore()" },
                new[] { "MustHaveHappened(Repeated.NoMoreThan.Once)", "MustHaveHappenedOnceOrLess()" },
                new[] { "MustHaveHappened(Repeated.Exactly.Twice)", "MustHaveHappenedTwiceExactly()" },
                new[] { "MustHaveHappened(Repeated.AtLeast.Twice)", "MustHaveHappenedTwiceOrMore()" },
                new[] { "MustHaveHappened(Repeated.NoMoreThan.Twice)", "MustHaveHappenedTwiceOrLess()" },
                new[] { "MustHaveHappened(Repeated.Exactly.Times(4))", "MustHaveHappened(4, Times.Exactly)" },
                new[] { "MustHaveHappened(Repeated.AtLeast.Times(5))", "MustHaveHappened(5, Times.OrMore)" },
                new[] { "MustHaveHappened(Repeated.NoMoreThan.Times(6))", "MustHaveHappened(6, Times.OrLess)" },
                new[] { "MustHaveHappened(Repeated.Like(n => n % 2 == 0))", "MustHaveHappenedANumberOfTimesMatching(n => n % 2 == 0)" },
            };
    }
}
