using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.FPS.Game;
using Unity.FPS.Game.Editor;

namespace Unity.FPS.Tests
{
    /// <summary>
    /// Verifies the ReqToCode traceability chain: requirement sources parse, the generated
    /// SWR traceables match them, and every approved requirement with a required trace is
    /// referenced by a [Traces] attribute in code.
    /// </summary>
    public class ReqToCodeTests
    {
        [Test]
        public void RequirementSources_ParseWithoutErrors()
        {
            ReqToCodeGenerator.ParseRequirements(out List<string> errors);
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void RequirementSources_ExistAndHaveUniqueIds()
        {
            var requirements = ReqToCodeGenerator.ParseRequirements(out _);
            Assert.IsNotEmpty(requirements, "No requirement documents with req-id frontmatter found under Docs/requirements.");
            Assert.AreEqual(requirements.Count, requirements.Select(r => r.Number).Distinct().Count(),
                "Requirement IDs must be unique.");
        }

        [Test]
        public void GeneratedTraceables_AreUpToDate()
        {
            bool upToDate = ReqToCodeGenerator.IsUpToDate(out string reason);
            Assert.IsTrue(upToDate, reason);
        }

        [Test]
        public void Traceability_HasNoViolations()
        {
            ReqToCodeVerifier.Report report = ReqToCodeVerifier.Verify();
            Assert.IsEmpty(report.Errors, string.Join("\n", report.Errors));
        }

        [Test]
        public void Generator_EmitsObsolete_ForDeprecatedRequirements()
        {
            string source = ReqToCodeGenerator.GenerateSource(new[]
            {
                FakeRequirement(999, RequirementStatus.Deprecated)
            });

            StringAssert.Contains("[Obsolete(\"SWR-999 is deprecated:", source);
            StringAssert.Contains("SWR_999 = 999,", source);
        }

        [Test]
        public void Generator_DoesNotEmitObsolete_ForApprovedOrDraftRequirements()
        {
            string source = ReqToCodeGenerator.GenerateSource(new[]
            {
                FakeRequirement(998, RequirementStatus.Approved),
                FakeRequirement(999, RequirementStatus.Draft)
            });

            StringAssert.DoesNotContain("[Obsolete(", source);
        }

        [Test]
        public void Generator_EmitsTestRequiredFlag()
        {
            var required = FakeRequirement(999, RequirementStatus.Approved);
            required.TestRequired = true;
            var optional = FakeRequirement(998, RequirementStatus.Approved);
            optional.TestRequired = false;

            string source = ReqToCodeGenerator.GenerateSource(new[] { optional, required });

            StringAssert.Contains("\"Docs/requirements/fake/999.md\", false, \"0000000000000000\", true)]", source);
            StringAssert.Contains("\"Docs/requirements/fake/998.md\", false, \"0000000000000000\", false)]", source);
        }

        [Test]
        public void Verifier_CountsTestCoverage_FromVerifiesAttributes()
        {
            ReqToCodeVerifier.ReferenceMap references = ReqToCodeVerifier.CollectReferences();

            Assert.IsTrue(references.Verifies.ContainsKey(SWR.SWR_101),
                "[Verifies] on tests in this assembly must be collected as test coverage.");
            Assert.IsTrue(references.Traces.ContainsKey(SWR.SWR_101),
                "[Traces] on implementation code must still be collected separately.");
        }

        [Test]
        public void Generator_RemovedRequirement_RemovesTraceable()
        {
            string withRequirement = ReqToCodeGenerator.GenerateSource(new[]
            {
                FakeRequirement(998, RequirementStatus.Approved),
                FakeRequirement(999, RequirementStatus.Approved)
            });
            string withoutRequirement = ReqToCodeGenerator.GenerateSource(new[]
            {
                FakeRequirement(998, RequirementStatus.Approved)
            });

            StringAssert.Contains("SWR_999", withRequirement);
            StringAssert.DoesNotContain("SWR_999", withoutRequirement);
        }

        static ReqToCodeGenerator.ParsedRequirement FakeRequirement(int number, RequirementStatus status)
        {
            return new ReqToCodeGenerator.ParsedRequirement
            {
                Id = $"SWR-{number}",
                Number = number,
                Status = status,
                Title = "Fake requirement for generator tests",
                SourcePath = $"Docs/requirements/fake/{number}.md",
                TraceRequired = false,
                TestRequired = false,
                ContentHash = "0000000000000000"
            };
        }
    }
}
