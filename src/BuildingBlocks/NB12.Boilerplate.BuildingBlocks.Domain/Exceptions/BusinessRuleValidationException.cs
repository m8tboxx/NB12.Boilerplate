using NB12.Boilerplate.BuildingBlocks.Domain.Interfaces;

namespace NB12.Boilerplate.BuildingBlocks.Domain.Exceptions
{
    public class BusinessRuleValidationException : Exception
    {
        public IBusinessRule BrokenRule { get; }

        public string Details { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="BusinessRuleValidationException"/> for a violated business rule.
        /// </summary>
        /// <remarks>
        /// The exception message is taken from <paramref name="brokenRule"/>.<see cref="IBusinessRule.Message"/>.
        /// The violated rule is stored in <c>BrokenRule</c>, and <c>Details</c> is also populated with the same message
        /// to provide additional context for error handling and reporting.
        /// </remarks>
        /// <param name="brokenRule">The business rule that was violated.</param>
        public BusinessRuleValidationException(IBusinessRule brokenRule)
            : base(brokenRule.Message)
        {
            BrokenRule = brokenRule;
            this.Details = brokenRule.Message;
        }

        public override string ToString()
        {
            return $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
        }
    }
}
