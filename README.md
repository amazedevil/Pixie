Pixie is a lightweight framework for building server side for Unity game engine project. It's not intended for real time multiplayer, but rather for message exchange in some kind of match making system, or turn based multiplayer.

The main idea of this project, is to build ready-to-use infrastructure, similar to HTTP frameworks, like "Ruby on Rails", "Laravel" or "ASP.NET MVC" for building socket-based applications, talking to Unity clients.

## Quick start

Let's assume you have Unity project, that wants to connect to some server, and send message to other clients connected to it. Some kind of simple chat if you wish. First of all, we should build library with messages, that will be shared between Unity project code base and server code base. Messages may look like this:

```csharp
//Message sent from client to server
public struct ServerMessageSaySomething {
    public string Content;
}

//Message sent from server to client
public struct ClientMessageSaySomething {
    public string Content;
    public string Author;
}
```

After that, let's inherit PXServer by some new class and create initial options:

```csharp
class MyServer : PXServer
{
    public RTHSServer(IPXInitialOptionsService options) : base(options) {
    }

    //Here we should pass message handler (we'll look at ones later) types
    protected override Type[] GetMessageHandlerTypes() {
        return new Type[] {
            typeof(MessageHandlerSaySomething),
        };
    }
}

//We'll use this structure to pass on starting server
class MyInitialOptions : IPXInitialOptionsService {
    public int Port => 7777; //port server is listening to
    public bool Debug => false; //is debugging on, that may change server behaviour in some cases
    public string Host => "localhost"; //interface server is listening to
}
```

When it's ready, we run it:

```csharp
(new RTHSServer(new MyInitialOptions())).Start();
```

Let's see what MessageHandlerSaySomething consists of:

```csharp
class MessageHandlerSaySomething : PXMessageHandlerBase<ServerMessageSaySomething>
{
    public override void Handle(ServerMessageSaySomething data) {
        //this.context is current message handling scoped central DryIoc container,
        //you're about to take all needed services from it.
        //Here we use some extension shortcuts to Sender and Client,
        //you can make your own, or use pure DryIoc Resolve
        var sender = this.context.Sender();
        var client = this.context.Client();
        
        sender.Send(
            sender.GetClientIds(),
            new ClientMessageSaySomething() { Content = data.Content, Author = client.Id }
        );
    }
}
```

So what about Unity side, first of all we should import package "PixieUnity.unitypackage" (link to be added).

Now we're adding PXUnityClient component to some game object. It has fields to be configured:

- serverHost: hostname or ip of our server
- serverPort: port our server is listening to
- eventKeepers: here we should add all game objects containing message handler components

At this point, shared messages library should be imported to Unity project, as we remember, it contains ClientMessageSaySomething structure, that is to be received from server. To make it happen, we should create message handler component:

```csharp
public class ClientSaySomethingMessageHandler : PXUnityMessageHandlerBase<ClientMessageSaySomething>
{
    [Serializable]
    public class MessageReceivedEvent : UnityEvent<ClientMessageSaySomething> { }

    //this field name is requred to be "messageReceivedEvent", as it called over reflection
    [SerializeField]
    public MessageReceivedEvent messageReceivedEvent;
}
```

As you can guess, messageReceivedEvent event is called on receiving ClientMessageSaySomething, so we need to attach some method to that event:

```csharp
public class SaySomethingLogicBehaviour : MonoBehaviour
{
    public void OnSaySomething(ClientMessageSaySomething message) {
        Debug.Log($"{message.Author}: {message.Content}");
    }
}
```

And, in the end we're about to make message sending from client, we can organize method calling in any way we want, but sending itself is going to be something like this:

```csharp
public class SenderBehaviour : MonoBehaviour
{
    [SerializeField]
    private PXUnityClient client; //here we should attach our PXUnityClient, mentioned above

    public void SendMessage(string message) {
        client.SendMessage(new ServerMessageSaySomething() { Content = message });
    }
}
```

That's it! We've made a simple chat with Pixie framework. If you're going to dive into some more topics, we'll talk about:

- making command line interface (CLI) commands to be able to interact with our running server (guide in development)
- scheduling jobs, to handle some heavy tasks in background, or just tasks, fireing not at the time of some message handling (e.g. cron scheduled) (guide in development)
- working with database over entity framework core, how link it with Pixie based server using middlewares and how to deal with middlewares in general (guide in development)

## Dependencies

- [DryIoc](https://github.com/dadhi/DryIoc) - used as dependency injection mechanism, framework is based on.
- [Newtonsoft.Json](https://www.newtonsoft.com/) - used for message encoding/decoding + environment parameters reading.
- [Quartz](https://www.quartz-scheduler.net/) - used for jobs scheduling.

## License

Pixie is open-sourced software licensed under the [MIT license](https://github.com/amazedevil/Pixie/blob/master/LICENSE.md).