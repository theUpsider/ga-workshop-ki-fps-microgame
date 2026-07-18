using System;

namespace Unity.FPS.Game
{
    /// <summary>
    /// Lifecycle status of a software requirement (SWR), mirrored from the
    /// `status:` frontmatter field in Docs/requirements.
    /// </summary>
    public enum RequirementStatus
    {
        /// <summary>Requirement exists but is not yet binding; tracing is not enforced.</summary>
        Draft = 0,

        /// <summary>Requirement is binding; if its trace is required, untraced code fails verification.</summary>
        Approved = 1,

        /// <summary>Requirement is phased out; every remaining reference produces an obsolete-warning.</summary>
        Deprecated = 2
    }

    /// <summary>
    /// Metadata the ReqToCode generator emits on every <see cref="SWR"/> member.
    /// The source of truth is the markdown file at <see cref="SourcePath"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class RequirementAttribute : Attribute
    {
        public string Id { get; }
        public RequirementStatus Status { get; }
        public string Title { get; }
        public string SourcePath { get; }

        /// <summary>When true, at least one [Traces] reference must exist in code.</summary>
        public bool TraceRequired { get; }

        /// <summary>Short hash of the requirement document; changes when the requirement text changes.</summary>
        public string ContentHash { get; }

        /// <summary>When true, at least one [Verifies] reference must exist in a test assembly.</summary>
        public bool TestRequired { get; }

        public RequirementAttribute(string id, RequirementStatus status, string title, string sourcePath,
            bool traceRequired, string contentHash, bool testRequired = true)
        {
            Id = id;
            Status = status;
            Title = title;
            SourcePath = sourcePath;
            TraceRequired = traceRequired;
            ContentHash = contentHash;
            TestRequired = testRequired;
        }
    }

    /// <summary>
    /// Links a code element to the requirement(s) it implements.
    /// This is a compile-time link, not a comment: if a requirement is removed
    /// from Docs/requirements, its SWR member disappears and every [Traces]
    /// reference to it becomes a build error. If a requirement is deprecated,
    /// every reference produces an obsolete-warning at that exact location.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
        AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Constructor |
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event,
        AllowMultiple = true, Inherited = false)]
    public sealed class TracesAttribute : Attribute
    {
        public SWR[] Requirements { get; }

        public TracesAttribute(params SWR[] requirements)
        {
            Requirements = requirements ?? Array.Empty<SWR>();
        }
    }

    /// <summary>
    /// Links a test to the requirement(s) whose behaviour it verifies (test coverage
    /// counterpart of <see cref="TracesAttribute"/>). Only references inside test
    /// assemblies count as coverage. Same compile-time lifecycle: removed requirement
    /// = build error, deprecated requirement = obsolete-warning on the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public sealed class VerifiesAttribute : Attribute
    {
        public SWR[] Requirements { get; }

        public VerifiesAttribute(params SWR[] requirements)
        {
            Requirements = requirements ?? Array.Empty<SWR>();
        }
    }
}
