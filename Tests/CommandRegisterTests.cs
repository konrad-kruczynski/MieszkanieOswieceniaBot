using System.Threading.Tasks;
using MieszkanieOswieceniaBot;
using MieszkanieOswieceniaBot.Commands;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public sealed class CommandRegisterTests
{
    [Test]
    public async Task ShouldFindAndExecuteCommand()
    {
        var message = ProduceFakeMessage("ping");
        await register.HandleTelegramMessageAsync(message);
        Assert.AreEqual("pong", fakeClient.LastText);
    }

    [Test]
    public async Task ShouldSuggestMostSimilarCommand()
    {
        var message = ProduceFakeMessage("pring");
        await register.HandleTelegramMessageAsync(message);
        Assert.AreNotEqual("pong", fakeClient.LastText);
        Assert.IsTrue(fakeClient.LastText.Contains("ping"));
    }

    [Test]
    public async Task ShouldDetectTooMuchArguments()
    {
        var message = ProduceFakeMessage("ping sth");
        await register.HandleTelegramMessageAsync(message);
    }

    [SetUp]
    public void SetUp()
    {
        fakeClient = new FakeBotClient();
        register = new CommandRegister(fakeClient);
        register.RegisterCommand("ping", new Ping());
        register.RegisterCommand("fake", new Ping());
    }

    public static Telegram.Bot.Types.Message ProduceFakeMessage(string text)
    {
        var message = new Telegram.Bot.Types.Message
        {
            Text = text,
            Chat = new Telegram.Bot.Types.Chat(),
            From = new Telegram.Bot.Types.User()
        };

        return message;
    }

    private CommandRegister register;
    private FakeBotClient fakeClient;
}

