using FluentValidation;

namespace testService
{
	public class TestRequestValidator : AbstractValidator<TestRequest>
	{
		public TestRequestValidator()
		{
			RuleFor(x => x.Age).NotEmpty();
			RuleFor(x => x.Desc).NotEmpty();
			RuleFor(x => x.IntroducerId).NotEmpty();
			RuleFor(x => x.Name).NotEmpty();
			RuleFor(x => x.Key).NotEmpty();
		}
	}
}