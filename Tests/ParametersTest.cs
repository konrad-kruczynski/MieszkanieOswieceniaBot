using System;
using MieszkanieOswieceniaBot.Commands;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public sealed class ParametersTest
	{
		[Test]
		public void ShouldDetectTooMuchParamaters()
		{
			var parameters = new TextCommandParameters("", new[] { "one", "two" });
			var exception = Assert.Throws<ParameterException>(parameters.ExpectNoOtherParameters);
			Assert.AreEqual(ParameterExceptionType.TooMuchParameters, exception.Type);
		}

		[Test]
		public void ShouldDetectNotEnoughParamaters()
		{
			var parameters = new TextCommandParameters("", new[] { "one" });
			parameters.TakeString();
			var exception = Assert.Throws<ParameterException>(() => parameters.TakeString());
			Assert.AreEqual(ParameterExceptionType.NotEnoughParameters, exception.Type);
		}

		[Test]
		public void ShouldConsumeString()
		{
			var parameters = new TextCommandParameters("", new[] { "one" });
			Assert.AreEqual("one", parameters.TakeString());
		}

		[Test]
		public void ShouldConsumeInteger()
		{
			var parameters = new TextCommandParameters("", new[] { "123" });
			Assert.AreEqual(123, parameters.TakeInteger());
		}

		[Test]
		public void ShouldReturnFalseOnTryWhenNotEnoughParameters()
		{
			var parameters = new TextCommandParameters("", Array.Empty<string>());
			Assert.IsFalse(parameters.TryTakeString(out _));
		}

		[Test]
		public void ShouldConsumeEnum()
		{
			var parameters = new TextCommandParameters("", new[] { "one" });
			Assert.AreEqual(SomeEnum.One, parameters.TakeEnum<SomeEnum>());
		}

		[Test]
		public void ShouldGivePositionOfTheFailingParameter()
		{
			var parameters = new TextCommandParameters("", new[] { "1", "a", "2" });
			Assert.AreEqual(1, parameters.TakeInteger());
			var exception = Assert.Throws<ParameterException>(() => parameters.TakeInteger());
			Assert.AreEqual(ParameterExceptionType.ConversionError, exception.Type);
			Assert.AreEqual(2, exception.Position);
		}
	}
}

