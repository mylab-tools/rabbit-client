# MyLab.RabbitClient
[![NuGet Version and Downloads count](https://buildstats.info/nuget/MyLab.RabbitClient)](https://www.nuget.org/packages/MyLab.RabbitClient)

```
Поддерживаемые платформы: .NET Core 3.1, .NET 5.0
```
Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор

### Как опубликовать?

Пример интеграции в приложение:

```c#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRabbit();						// 1
        services.ConfigureRabbit(Configuration);	// 2
    }
}
```

, где:

* `1` - добавление сервисов работы с `Rabbit`;
* `2` - конфигурирование сервисов работы с `Rabbit`;

Пример публикации сообщения:

```C#
class Service
{
    IRabbitPublisher _mq;
    
	public Service(IRabbitPublisher mq)	// 1
    {
		_mq = mq;
	}
    
    public SendMessage(string msgContent)
    {
        _mq.IntoQueue("my-test-queue")	// 2
           .SendString(msgContent)		// 3
           .Publish();					// 4
    }
}
```

, где:

* `1` - инъекция сервиса публикации сообщений;

* `2` - указание целевого объекта публикации;
* `3` - указание содержательной части сообщения;
* `4` - публикация!

### Как потребить?

Пример интеграции в приложение:

```c#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRabbit();								// 1
        services.ConfigureRabbit(Configuration);			// 2
        services.AddRabbitConsumer<MyConsumer>("my-queue");	// 3
    }
}
```

, где:

* `1` - добавление сервисов работы с `Rabbit`;
* `2` - конфигурирование сервисов работы с `Rabbit`;
* `1` - добавление потребителя сообщений очереди `my-queue`.

Пример потребителя сообщения:

```C#
class MyConsumer : RabbitConsumer<string>
{
    protected override Task ConsumeMessageAsync(ConsumedMessage<string> consumedMessage)
    {
		//do something
    }
}
```

## Интеграция

Интеграция осуществляется стандартным способом - добавлением сервисов библиотеки в коллекцию сервисов в методе  `ConfigureService` стартового класса приложения.

```C#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        //...
        
        serviceCollection.AddRabbit();
         
        serviceCollection.AddRabbit(RedisConnectionStrategy.Lazy);
            
        //...
    }
}
```

В методе `AddRedis` можно указать стратегию подключения к `Rabbit`  типа `RabbitConnectionStrategy`:

* `Lazy` - (по умолчанию) подключение устанавливается при попытке отправить запрос в ``Rabbit`  `, если подключение не установлено. Особенности:
  * происодит блокировка потоков в момент проверки состояния подключения и на время процесса подключения;
  * попытка подключения происходит по требованию: если `Rabbit` не используется, то и подключение не будет установлено;
* `Background` - подключение устанавливается сразу после запуска приложения. Особенности:
  * при неудачах, повторные попытки первичного подключения будут повторяться бесконечно;
  * при разрыве по инициативе сервера, будут осуществляться бесконечные попытки восстановить подключение;
  * при попытке сделать запрос в `Rabbit`, если подключение не установлено, будет выдано исключение `RabbitConnectionStrategy`.

## Конфигурация

### Параметры конфигурации

Объект конфигурации `MyLab.RabbitClient` имеет следующее содержание:

* `Host` - хост подключения;
* `VHost` - виртуальный хост
* `Port` - порт. `5672` - по умолчанию;
* `User` - имя пользователя;
* `Password` - пароль;
* `BackgroundRetryPeriodSec` - период повторных попыток подключения в фоновом режиме. 10 по умолчанию;
* `Pub[]`: - настройки публикации
  * `Id` - идентификатор 
  * `Exchange` - имя обменника или не указан, если публикация в очередь;
  * `RoutingKey` - ключ маршрутизации или имя очереди, если публикация в очередь;
* `DefaultPub`: - настройка публикации по умолчанию
  * `Exchange` - имя обменника или не указан, если публикация в очередь;
  * `RoutingKey` - ключ маршрутизации или имя очереди, если публикация в очередь.

### Методы конфигурирования

#### Конфигурация приложения  

```C#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureRabbit(Configuration);				// "MQ" by default
        services.ConfigureRabbit(Configuration, "Rabbit");	
    }
}
```

При использовании такого подхода для конфигурирования используется именованный узел конфигурации приложения (`MQ` по умолчанию).

#### В коде

```C#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureRabbit(opt => opt.Host = "localhost");
    }
}
```

 Частичная или полное определение конфигурации в коде.

#### Любые другие способы

Доступны любые другие поддерживаемые в `.NET Core 3.1+`  способы конфигурирования с использованием объекта [MyLab.RabbitClient.RabbitOptions](./src/MyLab.RabbitClient/RabbitOptions.cs).

## Публикация

### Подключение публикации

Подключение публикации происходит единственным образом:

```C#
services.AddRabbitPublisher();
```

### DSL публикация 

Публикацию сообщений осуществляет сервис, реализующий интерфейс `IRabbitPublisher`.

Для публикации, необходимо "собрать" сообщение, используя `DSL` образные выражения из вызовов методов этого сервиса.

Публикация сообщения выполняется в несколько этапов:

*  определение настроек публикации:

  * `IntoDefault` - настройки берётся из объекта конфигурации из поля `DefaultPub`:

    ```C#
    IntoDefault(string routingKey = null)
    ```

  * `IntoQueue(string queue)` - публикация в очередь:

    ```C#
    IntoQueue(string queue)
    ```

  * `IntoExchange` - публикация в обменник:

    ```C#
    IntoExchange(string exchange, string routingKey = null)
    ```

  * `Into<T>` - настройки берутся из объекта конфигурации из поля `Pub[]`. Идентификатор объекта настройки берётся из атрибута `RabbitConfigIdAttribute` модели сообщения:

    ```C#
    Into<TMsg>(string routingKey = null);
    ```

    ```C#
    [RabbitConfigId("mymsg")]
    class MyMessage
    {    
    }
    ```

    ```json
    {
        "Host": "localhost",
        "Pub": {
            "Id": "mymsg",
            "RoutingKey": "my-queue"
        }
    }
    ```

  * `Into` - настройки берутся из объекта конфигурации из поля `Pub[]`. Идентификатор объекта настройки берётся из параметров метода:

    ```C#
    Into(string configId, string routingKey = null);
    ```

* определение свойств и заголовков сообщения:

  * `AndProperty` - позволяет определить несколько [базовых параметров](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/projects/RabbitMQ.Client/client/api/IBasicProperties.cs) сообщения; 
  * `AndHeader` - позволяет добавить заголовок сообщения;

* определение содержимого сообщения:

  * `SendJson` - передача объекта в формате `json`:

    ```c#
    SendJson(object obj)
    ```

  * `SendBinary` - передача бинарных данных:

    ```C#
    SendBinary(byte[] binData)
    ```

  * `SendString` - передача строки:

    ```C#
    SendString(string strData)
    ```

* `Publish` - непосредственно, публикация.

Пример публикации сообщения:

```c#
publisher
  .IntoQueue(queue.Name)
  .SendJson(testEntity)
  .Publish();
```

### Контекст отправки

Поддерживается установка контекста при отправке сообщения. Все контексты устанавливаются перед отправкой каждого сообщения. А после отправки - освобождаются.

При установке контекста доступны для изменения все параметры отправляемого сообщения.

Для внедрения контекста необходимо:

* реализовать класс контекста отправки, реализующий `IPublishingContext`:

  ```C#
  /// <summary>
  /// Set context for publishing message
  /// </summary>
  public interface IPublishingContext
  {
      /// <summary>
      /// Set context
      /// </summary>
      IDisposable Set(RabbitPublishingMessage publishingMessage);
  }
  ```

* в методе `Set` выполнять действия, связанные с установкой контекста и возвращать объект `IDisposable`, у которого по завершению отправки сообщения будет вызван метод `Dispose`. Для случаев, когда не требуются действия при освобождении контекста, можно возвращать статический экземпляр пустого объекта контекста `EmptyPubCtx.Instance`;

* зарегистрировать его в сервисах приложения:

  ```C#
  services.AddRabbitPublishingContext<MyCustomPubCtx>();
  ```

Объект контекста отправления сообщений имеет время жизни `singleton`.

## Потребление

### Потребители

При включении механизмов потребления сообщений очереди, необходимо указать потребителей.

Потребитель - класс, объекты которого используются для обработки полученных из очереди сообщений. Класс потребителя должен реализовывать интерфейс `MyLab.RabbitClient.Consuming.IRabbitConsumer`, для которого имеется удобная абстрактная реализация `MyLab.RabbitClient.Consuming.RabbitConsumer<TContent>`. Ниже представлен пример использование этой реализации:

```C#
class MyConsumer : RabbitConsumer<string>
{
    protected override Task ConsumeMessageAsync(ConsumedMessage<string> consumedMessage)
    {
		// do something
    }
}
```

 ### Регистрация потребителя

#### Регистрация объекта потребителя

```C#
services.AddRabbitConsumer("my-queue", new MyConsumer());
```

В этом случае, объект потребителя регистрируется как `Singleton` и используется в единственном экземпляре для обработки всех сообщений указанной очереди.

#### Регистрация типа потребителя

```C#
services.AddRabbitConsumer<MyConsumer>("my-queue");
```

При такой регистрации для обработки каждого сообщения указанной очереди будет создаваться объект потребителя указанного типа. При создании используется стандартная фабрика `.NET 5` с инъекцией сервисов приложения. 

#### Регистрация по опциям

```C#
services.AddRabbitConsumer<MyOptions, MyConsumer>(opt => opt.QueueName, optional:true);
```

Этот метод регистрации потребителя позволяет получить имя очереди из специфичных для приложения опций. 

Параметр `optional` (`false` по умолчанию) позволяет реализовать опциональную регистрацию потребителя в случае, если надо потреблять сообщения из очереди только если она указана в конфигурации.

#### Регистратор потребителей

В общем случае, в приложении необходимо зарегистрировать несколько потребителей, которые получают сообщения из очередей, имена которых можно получить на этапе, после инициализации приложения, например из конфигурации или из других сервисов. Такую регистрацию могут осуществить регистраторы потребителей. 

Регистратор потребителя добавляется как объект:

```C#
services.AddRabbitConsumers(new MyRegistrar());
```

Или может добавляться как тип класса регистратора, объект которого будет создан и использован после инициализации приложения с применением инъекции сервисов приложения:

```C#
services.AddRabbitConsumers<MyRegistrar>();
```

Пример регистратора потребителей:

```C#
class MyConsumerRegistrar : IRabbitConsumerRegistrar
{
    MyOptions _opts;
    
    public MyConsumerRegistrar(IOptions<MyOptions> opts)
    {
        _opts = opts.Value;
    }
    
	public void Register(IRabbitConsumerRegistry registry, IServiceProvider serviceProvider)
    {
        registry.Register(_opts.Queue1, new TypedConsumerProvider<MyConsumer>());
        
        var consumer = ActivatorUtilities.CreateInstance<TConsumer>(serviceProvider);
        registry.Register(_opts.Queue2, new SingleConsumerProvider(consumer));
    }
}
```

### Внедрение в обработку

Поддерживается внедрение в обработку полученного сообщения. Можно изменять все свойства объекта [BasicDeliverEventArgs](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/master/projects/RabbitMQ.Client/client/events/BasicDeliverEventArgs.cs).

Для этого потребуется:

* реализовать класс логики обработки, реализующий интерфейс `IConsumedMessageProcessor`:

  ```C#
  /// <summary>
  /// Processed consumed message
  /// </summary>
  public interface IConsumedMessageProcessor
  {
      /// <summary>
      /// Processes consumed message
      /// </summary>
      public void Process(BasicDeliverEventArgs deliverEventArgs);
  }
  ```

* добавить его в сервисы приложения:

  ```c#
  services.AddRabbitConsumedMessageProcessor<MyCustomMessagePRocessor>();
  ```

Объект логики обработки отправляемых сообщений имеет время жизни `scoped`.

## Объектная модель RabbitMQ

### Обменник 

В объектной модели, обменник представлен классом `RabbitExchange`. 

Для создания обменника, необходимо воспользоваться фабрикой `RabbitExchangeFactory`. Фабрика позволяет указать параметры создания обменника и создать его. 

Ниже приведён пример создания обменника для тестирования:

```c#
var exchangeFactory = new RabbitExchangeFactory(RabbitExchangeType.Fanout, _channelProvider)
{
    AutoDelete = true,
    Prefix = "test-"
};

var exchange = exchangeFactory.CreateWithRandomId();
```

Здесь создаётся обменник типа `Fanout`, который будет автоматически удалён после отключения приложения от `RabbitMQ`. Имя обменника будет иметь префикс `test-`, а содержательная часть имени будет строковым представлением `guid` в формате без разделителей ( [формат N](https://docs.microsoft.com/ru-ru/dotnet/api/system.guid.tostring?view=net-5.0#System_Guid_ToString_System_String_) ). Пример имени: `test-2b1799b2ef414f66a4e0d64c5a8bd660`. 

Обменник поддерживает следующие операции:

* `Publish` - публикация объекта в формате `json`:

  ```C#
  Publish(object message, string routingKey = null)
  ```

* `IsExists` - проверка существования обменника:

  ```C#
  bool IsExists()
  ```

* `Remove` - удаление обменника:

  ```C#
  void Remove()
  ```

### Очередь

В объектной модели, очередь представлена классом `RabbitQueue`. 

Для создания очереди, необходимо воспользоваться фабрикой `RabbitQueueFactory`. Фабрика позволяет указать параметры создания очереди и создать её. 

Ниже приведён пример создания очереди для тестирования:

```C#
var queueFactory = new RabbitQueueFactory(TestTools.ChannelProvider)
{
    AutoDelete = true,
    Prefix ="test-"
};

var queue = queueFactory.CreateWithRandomId();
```

Здесь создаётся очередь, которая будет автоматически удалена после отключения приложения от `RabbitMQ`. Имя очереди будет иметь префикс `test-`, а содержательная часть имени будет строковым представлением `guid` в формате без разделителей ( [формат N](https://docs.microsoft.com/ru-ru/dotnet/api/system.guid.tostring?view=net-5.0#System_Guid_ToString_System_String_) ). Пример имени: `test-2b1799b2ef414f66a4e0d64c5a8bd660`. 

Очередь поддерживает следующие операции:

* `Publish` - публикация сообщений:

  ```C#
  void Publish(object message)
  ```

* `Listen<T>` - ожидание одного сообщения из очереди, которое будет десериализовано в объект указанного типа:

  ```C#
  ConsumedMessage<T> Listen<T>(TimeSpan? timeout = null)
  ```

* `BindToExchange` - подключение к обменнику:

  ```C#
  void BindToExchange(string exchangeName, string routingKey = null)
  void BindToExchange(RabbitExchange exchange, string routingKey = null)
  ```

* `IsExists` - проверка существования очереди:

  ```C#
  bool IsExists()
  ```

* `Remove` - удаление очереди:

  ```C#
  void Remove()
  ```

### Проверка работоспособности

Проверка работоспособности заключается в проверке наличия подключения к `RabbitMQ` и осуществляется через механизм [HealthCheck](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-3.1).

Для подключения проверки необходимо использовать метод `AddRabbit` для построителя проверок, как показано на примере ниже:

```c#
services.AddHealthChecks().AddRabbit();
```



