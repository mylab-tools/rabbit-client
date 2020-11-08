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

## Потребление

Для потребления сообщения необходимо:

* загрузить конфигурацию подключения  ([Конфигурирование](#Конфигурирование));
* реализовать логику потребления сообщений

* зарегистрировать потребителей.

### Логика потребления

Логика потребление - реализация обработки полученных сообщений. Реализация логики - класс, реализующий интерфейс `IMqConsumerLogic<TPayload>` для обработки сообщений по одному, и `IMqBatchConsumerLogic<TPayload>` для реализации логики обработки нескольких сообщений одновременно.

Вот эти интерфейсы, для ознакомления:

```C#
/// <summary>
/// Represent messages queue consumer
/// </summary>
public interface IMqConsumerLogic<TMsgPayload>
{
	Task Consume(MqMessage<TMsgPayload> message);
}

/// <summary>
/// Represent batch messages queue consumer
/// </summary>
public interface IMqBatchConsumerLogic<TMsgPayload>
{
	Task Consume(IEnumerable<MqMessage<TMsgPayload>> messages);
}
```

### Потребители

Потребитель - один из наследников класса `MqConsumer`. В общем случае не требуется разрабатывать наследника для решения регулярных задач. Для этого есть готовые реализации:

* `MqConsumer<TMsgPayload, TLogic>` - определяет обычного потребителя сообщений;
* `MqBatchConsumer<TMsgPayload, TLogic>` - определяет потребителя сообщений, получающего по несколько сообщений сразу.

Здесь:

* `TMsgPayload` - тип бизнес-сообщения;
* `TLogic` - тип логики потребления.

### Регистрация потребителей

Регистрация потребителей осуществляется с помощью метода расширения для `IServiceCollection`: 

```C#
public void ConfigureServices(IServiceCollection services)
{
	...
    services.AddMqConsuming(registrar =>
    {
    	registrar.RegisterConsumer(
    		new MqConsumer<MsgPayload,MyConsumerLogic>(
    			"my:test-queue");
    })
    ...
}

class MsgPayload
{
	public string Value { get; set; }
}

class MyConsumerLogic : IMqConsumerLogic<MsgPayload>
{
	Task Consume(MqMessage<MsgPayload> message)
	{
		// do something
	}
}
```

В случае, если наименование или дополнительные опции создания потребителя содержатся в конфигурации, регистрация потребителя может выглядеть следующим образом:

```C#
services.AddMqConsuming(r =>
	r.RegisterConsumerByOptions<MyOptions>(
       opt => new MqConsumer<MsgPayload,MyConsumerLogic>(opt.Queue)
                )
```

  

## Конфигурирование 

Конфигурирование позволяет загрузить параметры подключения к MQ серверу и автоматически их применять для публикации и потребления сообщений.

Объектная модель опций подключения выглядят следующим образом:

```C#
/// <summary>
/// Contains MQ connection options
/// </summary>
public class MqOptions
{
    /// <summary>
    /// Server host
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VHost { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; set; } = 5672;
    /// <summary>
    /// Login user
    /// </summary>
    public string User { get; set; }
    /// <summary>
    /// Login password
    /// </summary>
    public string Password { get; set; }
}
```

В приложении конфигурирование осуществляется двумя методами расширения `IServiceCollection`:

* `LoadMqConfig` - загружает настройки из конфигурации с возможностью указания имени узла (**Mq** - по умолчанию);
* `ConfigureMq` - определяет настройки через делегат.

```C#
public void ConfigureServices(IServiceCollection services)
{
	...
    services.LoadMqConfig(Configuration)
    ...
    services.ConfigureMq(o => 
    {
    	o.Host = "myhost.com";
		o.VHost = "test-host";
        o.User = "foo";
        o.Password = "foo-pass";
    });
    ...
}
```

Пример конфигурационного файла с портом по умолчанию:

```json
{
	"Mq": {
		"Host" : "myhost.com",
		"VHost" : "test-host",
        "User" : "foo",
        "Password" : "foo-pass"
	}
}
```

## Функциональное тестирование

### Эмулятор сообщений

Для функционального тестирования приложения, осуществляющего обработку сообщений из очередей на базе `MyLab.Mq`, рекомендуется использовать `эмулятор входящих сообщений` (сервис с интерфейсом `IInputMessageEmulator`).

```C#
/// <summary>
/// Specifies emulator of queue with input messages
/// </summary>
public interface IInputMessageEmulator
{
    /// <summary>
    /// Emulates queueing of message 
    /// </summary>
    public Task<FakeMessageQueueProcResult> Queue(object message, string queue, IBasicProperties messageProps = null);
}

/// <summary>
/// Contains fake queue message processing result
/// </summary>
public class FakeMessageQueueProcResult
{
    /// <summary>
    /// Is there was acknowledge
    /// </summary>
    public bool Acked { get; set; }

    /// <summary>
    /// Is there was rejected
    /// </summary>
    public bool Rejected { get; set; }

    /// <summary>
    /// Exception which is reason of rejection
    /// </summary>
    public Exception RejectionException { get; set; }

    /// <summary>
    /// Requeue flag value
    /// </summary>
    public bool RequeueFlag { get; set; }
}
```

Для этого необходимо:

* при конфигурировании приложения зарегистрировать эмулятор в сервисах:

  ```C#
  services
      .AddMqConsuming(cr => cr.RegisterConsumer(consumer))
      .AddMqMsgEmulator();  // <----
  ```
  
  При этом не будет осуществляться подключение к реальной очереди для прослушивания очередей.

* получить эмулятор `IInputMessageEmulator` из поставщика сервисов:

  ```C#
  var services = new ServiceCollection();
  
  ...
  
  services
      .AddMqConsuming(cr => cr.RegisterConsumer(consumer))
      .AddMqMsgEmulator();
  
  var srvProvider = services.BuildServiceProvider();  
  
  var emulator = srvProvider.GetService<IInputMessageEmulator>(); // <----
  ```
  
  Или в конструкторе объекта, создаваемого с использованием `DI`
  
* отправить тестовое сообщение:

  ```C#
  await emulator.Queue(testMsg, "foo-queue");
  ```

При отправке через эмулятор, сообщение обрабатывается синхронно. Результатом обработки сообщения является объект типа `FakeMessageQueueProcResult` (представлен выше). Он и является предметом анализа в тесте.

### Объект логики потребителя

При тестировании рекомендуется рассмотреть использование единого экземпляра логики потребителя. Т.е. при регистрации создать объект логики потребителя и передать в потребитель, вместо подхода когда потребитель сам создаёт логику. При этом появляется возможность самостоятельно инициализировать объект логики, кастомизировав его, например, тестовым поведением.

```C#
var services = new ServiceCollection();

var logic = new TestConsumerLogic();
var consumer = new MqConsumer<TestEntity, TestConsumerLogic>("foo-queue", logic); // <----

var emulatorRegistrar = new InputMessageEmulatorRegistrar();

services.AddMqConsuming(
    consumerRegistrar => consumerRegistrar.RegisterConsumer(consumer),
    emulatorRegistrar
);
```

Объект логики и/или его тестовые зависимости являются предметом анализа в тесте.

## Интеграционное тестирование

При тестировании с реальным сервисом `RabbitMQ` в тестах может потребоваться выполнить некоторые действия без интеграции в `.NET Core` приложение и без связи с какими-то зависимостями.

### Создание очереди

Класс `MqQueueFactory` - фабрика очередей. Создаёт очередь с указанными характеристиками.

Для целей тестирования, рекомендуется инициализировать фабрику, указывая префикс имён очередей и флаг `AutoDelete` :

```C#
var queueFactory = new MqQueueFactory(connProvider)
{
    Prefix = "prefix:",
    AutoDelete = true
};
```

У фабрики есть несколько способов назначения имён создаваемым очередям:

* **указать точное имя**

  ```C#
  MqQueue queue = queueFactory.CreateWithName("foo");
  //name: 'foo'
  //ignore prefix!!
  ```

* **указать идентификатор**

  ```C#
  MqQueue queue = queueFactory.CreateWithId("foo");
  //name: 'prefix:foo'
  ```

* **назначить случайный идентификатор**

  ```C#
  MqQueue queue = queueFactory.CreateWithRandomId();
  //name: 'prefix:4a2943bdfdc5434fa134c2c018635fea'
  ```

### Создание обменника

Класс `MqExchangeFactory` - фабрика обменников. Создаёт обменник с указанными характеристиками.

Для целей тестирования, рекомендуется инициализировать фабрику, указывая префикс имён обменников и флаг `AutoDelete` :

```C#
var exchangeFactory = new MqExchangeFactory(MqExchangeType.Fanout, connProvider)
{
    Prefix = "prefix:",
    AutoDelete = true
};
```

У фабрики есть несколько способов назначения имён создаваемым обменникам:

* **указать точное имя**

  ```C#
  MqExchange exchange = exchangeFactory.CreateWithName("foo");
  //name: 'foo'
  //ignore prefix!!
  ```

* **указать идентификатор**

  ```C#
  MqExchange exchange = exchangeFactory.CreateWithId("foo");
  //name: 'prefix:foo'
  ```

* **назначить случайный идентификатор**

  ```C#
  MqExchange exchange = exchangeFactory.CreateWithRandomId();
  //name: 'prefix:4a2943bdfdc5434fa134c2c018635fea'
  ```

### Привязка очереди к обменнику

В примере ниже показано как осуществляется привязка очереди к обменнику:

```C#
MqQueue queue = ...
MqExchange exchange = ...

queue.BindToExchange(exchange, "foo-routing");
```

### Публикация

Публикация сообщения в очередь типа `MqQueue` осуществляется через метод `Publish`, в который можно передать произвольный объект, который будет сериализован в JSON и передан в очередь.

```C#
class Model
{
	public string Value { get;set; } = 10
}

//... 
    
queue.Publish(new Model());

//MQ Message content: {"Value":10}
```

### Потребление

Класс `MqQueue` предоставляет возможность синхронного чтения одного сообщения из очереди: 

```C#
MqMessage<TModel> next = queue.Listen<TModel>();
```

Ест возможность указать таймаут ожидания:

```C#
MqMessage<TModel> next = queue.Listen<TModel>(TimeSpan.FromSeconds(2));
```

Таймаут по умолчанию - 1 сек. В случае истечения заданного времени таймаута, возникнет исключение типа `TimeoutException`.