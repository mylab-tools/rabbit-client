# MyLab.Mq
[![NuGet Version and Downloads count](https://buildstats.info/nuget/MyLab.Mq)](https://www.nuget.org/packages/MyLab.Mq)

```
Поддерживаемые платформы: 
.NET Core 3.1+
```
Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор

`MyLab.Mq` - библиотека, содержащая инструменты для работы с очередью сообщений в реализации `RabbitMQ`. Разработана на базе официального клиента [RabbitMQ.NET](https://github.com/rabbitmq/rabbitmq-dotnet-client).

Аспекты организации отправки сообщений:

* объявлении объектной модели сообщения;
* привязка объектной модели сообщения к объекту очереди: очередь/обменник;
* загрузка конфигурации;
* регистрация реализации отправителя сообщений в DI-контейнере;
* получение отправителя сообщений в качестве зависимости;
* отправка объектов сообщения.

Аспекты организации получения сообщений:

* объявлении объектной модели сообщения;
* привязка объектной модели сообщения к объекту очереди: очередь/обменник;
* загрузка конфигурации;
* регистрация потребителей сообщений;
* обработка полученных сообщений в потребителях.

## Модель бизнес-сообщения

В качестве содержательной части сообщения можно передавать любое значение или объект. 

Модель сообщения, описанная в виде класса может быть помечена атрибутом `MqAttribute`, который позволяет указать для модели некоторые параметры по умолчанию:

* имя обменника;
* имя роутинга.

Ниже представлены примеры использования атрибута `MqAttribute`:

* по умолчанию сообщение будет отправляться в очередь `my:test-queue`

```C#
[Mq(Routing = "my:test-queue")]
class MsgPayload
{
	public string Value { get; set; }
}
```

* по умолчанию сообщение будет отправляться в обменник `my:test-exch` с пустым ключом роутинга

```C#
[Mq(Exchange = "my:test-exch")]
class MsgPayload
{
	public string Value { get; set; }
}
```

* по умолчанию сообщение будет отправляться в обменник `my:test-exch` с ключом роутинга `foo`

```C#
[Mq(Exchange = "my:test-exch", Routing = "foo")]
class MsgPayload
{
	public string Value { get; set; }
}
```

## MQ сообщение

В инфраструктуре сообщение представляется в виде класса `MqMessage<TPayload>`, где `TPayload` - тип модели бизнес-сообщения. Он используется как для отправки, так и для получения сообщений. Содержит обычные реквизиты для сообщения в контексте MQ. 

Объектная модель для ознакомления:

```C#
/// <summary>
/// Contains MQ message data
/// </summary>
/// <typeparam name="T">payload type</typeparam>
public class MqMessage<T>
{
    /// <summary>
    /// Message identifier. <see cref="Guid.NewGuid"/> by default
    /// </summary>
    public Guid MessageId { get; set; }
    /// <summary>
    /// Message correlated to this one
    /// </summary>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Gets response publish parameters
    /// </summary>
    public string ReplyTo { get; set; }
    /// <summary>
    /// Headers
    /// </summary>
    public MqHeader[] Headers { get; set; }
    /// <summary>
    /// Message payload
    /// </summary>
    public T Payload { get; }
    
    /// <summary>
    /// Initializes a new instance of <see cref="MqMessage{T}"/>
    /// </summary>
    public MqMessage(T payload)
    {
     	//....   
    }
}

/// <summary>
/// Represent MQ message header
/// </summary>
public class MqHeader
{
    /// <summary>
    /// Header name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Header value
    /// </summary>
    public string Value { get; set; }
}
```

## Публикация

Для публикации сообщения необходимо:

* загрузить конфигурацию подключения  ([Конфигурирование](#Конфигурирование))
* добавить сервис публикации сообщений

```C#
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddMqPublisher();
    ...
}
```

* объявить объектную модель сообщения ([Модель бизнес-сообщения](#модель-бизнес-сообщения))
* сервис отправления, как зависимость в классе-потребителе
* опубликовать сообщение.

Пример сервиса, отправляющего сообщение с помощью сервиса публикации сообщений:

```C#
public class SomeService
{
	IMqPublisher _mqPublisher

	public SomeService(IMqPublisher mqPublisher)
	{
		_mqPublisher = mqPublisher;
	}
	
	public void DoSomething()
	{
		....
		_mqPublisher.Publish(new MsgPayload { Value = "foo-val" });
		...
	}
}
```

В примеры выше представлена весьма развёрнутая и наполненная дополнительными опциями публикация. Ниже приведён список некоторых вариантов публикации сообщений:

* публикация объекта сообщения в очередь/обменник по умолчанию 

```C#
//Full
publisher.Publish(new OutgoingMqEnvelop<Msg>
{
    Message = new MqMessage<Msg>(
        new MsgPayload
        {
        	Value = "foo-val"
        })
});

//Short extension method
publisher.Publish(new MsgPayload { Value = "foo-val" });
```

* публикация с указанием целевой очереди: 

```C#
//Full
publisher.Publish(new OutgoingMqEnvelop<Msg>
{
    PublishTarget = new PublishTarget{ Routing = "my:another-queue" },
    Message = new MqMessage<Msg>(
        new MsgPayload
        {
        	Value = "foo-val"
        })
});

//Short extension method
publisher.PublishToQueue(new MsgPayload { Value = "foo-val" }, "my:another-queue");
```

* публикация с указанием целевого обменника:

 ```C#
//Full
publisher.Publish(new OutgoingMqEnvelop<Msg>
{
    PublishTarget = new PublishTarget { Exchange = "my:another-exch" },
    Message = new MqMessage<Msg>(
        new MsgPayload
        {
        	Value = "foo-val"
        })
});

//Short extension method
publisher.PublishToExchange(new MsgPayload { Value = "foo-val" }, "my:another-exch");
 ```

* публикация с указанием целевого обменника и ключа роутинга:

```C#
//Full
publisher.Publish(new OutgoingMqEnvelop<Msg>
{
    PublishTarget = new PublishTarget 
    { 
        Exchange = "my:another-exch" ,
        Routing = "foo"
    },
    Message = new MqMessage<Msg>(
        new MsgPayload
        {
        	Value = "foo-val"
        })
});

//Short extension method
publisher.PublishToExchange(
    new MsgPayload { Value = "foo-val" }, 
    "my:another-exch",
	"foo");
```

* публикация с указанием остальных параметров отправляемого сообщения:

```C#
publisher.Publish(new OutgoingMqEnvelop<Msg>
{
    PublishTarget = new PublishTarget
    {
        Routing = "my:test-queue"
    },
    Message = new MqMessage<Msg>(
        new MsgPayload
        {
        	Value = "foo-val"
        })
    {
        ReplyTo = "foo-queue",
        CorrelationId = correlationId,
        MessageId = messageId,
        Headers = new[]
        {
        	new MqHeader {Name = "FooHeader", Value = "FooValue"},
        }
    }
});
```

## Получение

## Конфигурирование 