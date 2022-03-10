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
			var parameters = new Parameters("", new[] { "one", "two" }, 0, 0);
			var exception = Assert.Throws<ParameterException>(parameters.ExpectNoOtherParameters);
			Assert.AreEqual(ParameterExceptionType.TooMuchParameters, exception.Type);
		}

		[Test]
		public void ShouldDetectNotEnoughParamaters()
		{
			var parameters = new Parameters("", new[] { "one" }, 0, 0);
			parameters.TakeString();
			var exception = Assert.Throws<ParameterException>(() => parameters.TakeString());
			Assert.AreEqual(ParameterExceptionType.NotEnoughParameters, exception.Type);
		}

		[Test]
		public void ShouldConsumeString()
		{
			var parameters = new Parameters("", new[] { "one" }, 0, 0);
			Assert.AreEqual("one", parameters.TakeString());
		}

		[Test]
		public void ShouldConsumeInteger()
		{
			var parameters = new Parameters("", new[] { "123" }, 0, 0);
			Assert.AreEqual(123, parameters.TakeInteger());
		}

		[Test]
		public void ShouldReturnFalseOnTryWhenNotEnoughParameters()
		{
			var parameters = new Parameters("", Array.Empty<string>(), 0, 0);
			Assert.IsFalse(parameters.TryTakeString(out _));
		}

		[Test]
		public void ShouldConsumeEnum()
		{
			var parameters = new Parameters("", new[] { "one" }, 0, 0);
			Assert.AreEqual(SomeEnum.One, parameters.TakeEnum<SomeEnum>());
		}

		[Test]
		public void ShouldGivePositionOfTheFailingParameter()
		{
			var parameters = new Parameters("", new[] { "1", "a", "2" }, 0, 0);
			Assert.AreEqual(1, parameters.TakeInteger());
			var exception = Assert.Throws<ParameterException>(() => parameters.TakeInteger());
			Assert.AreEqual(ParameterExceptionType.ConversionError, exception.Type);
			Assert.AreEqual(2, exception.Position);
		}
	}
}

