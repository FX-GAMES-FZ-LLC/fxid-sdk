# FXID: Подключите один раз - публикуйте везде

FXID - это интегрированное решение, которое позволяет разработчикам сфокусироваться на создании игр, пока мы берем на себя всю техническую сложность управления аккаунтами, платежами и интеграциями.

## Введение

FXID предоставляет разработчикам мощный инструментарий для интеграции их игр и приложений на множество платформ через единый API. Наше решение включает комплексный набор инструментов для разработки и тестирования, обеспечивая бесшовную публикацию на различных платформах.

## Ключевые компоненты

### Единая система аккаунтов
- Простое подключение и восстановление игровых аккаунтов без привязки к социальным сетям
- Безопасная email-верификация пользователей
- Автоматическая синхронизация прогресса между платформами

### Готовая система монетизации
- Встроенная платежная система с поддержкой multiple payment providers
- Гибкая настройка товаров и цен для разных регионов
- Автоматическая локализация товаров
- Аналитика продаж и поведения пользователей

### Интеграция с ключевыми платформами
- **Браузер**: Facebook, Facebook Instant Games, Kongregate, CrazyGames, WizQ, VK, VK Play, OK, Yandex Games и многие другие
- **Мобильные устройства**: App Store, Google Play, OneStore, Xsolla, Stripe, bank131, Youkassa и многие другие
- **ПК**: Steam, Epic Games Store, собственный лаунчер

### Дополнительные возможности
- Встроенная система рассылок и уведомлений
- Поддержка рекламных интеграций
- Система друзей и социальных механик
- Мультиязычность

### Аналитика и статистика
- Подробная аналитика и отчетность для отслеживания производительности ваших игр и приложений

### Техническая надежность
- Современный API с детальной документацией
- Поддержка всех популярных платформ и устройств
- Масштабируемая инфраструктура

### Эффективный CDN
- Быстрая и надежная доставка контента пользователям с помощью нашей оптимизированной сети CDN

### Персональный лаунчер
- Собственный лаунчер для ваших игр с возможностью кастомизации и брендинга

## Инструменты для разработчиков

FXID предоставляет два основных компонента для разработчиков:

### FXID Client SDK

Легковесный C# SDK для интеграции с сервисом FXID Profile.

#### Функции SDK
- **Автоматическое управление профилями**: Обработка обновлений и истечения срока действия профилей
- **Управление соединением**: Легкое подключение, отключение, приостановка и возобновление получения профилей
- **Интеграция Protocol Buffer**: Полная совместимость со схемой protobuf сервиса FXID Profile

#### Установка SDK

##### NuGet Package (Рекомендуется)
```bash
dotnet add package FxidClientSDK
```

##### Ручная установка
1. Клонируйте репозиторий клиентского SDK
2. Добавьте ссылку на проект в вашем решении
3. Добавьте необходимые зависимости:
   - System.Net.Http
   - Google.Protobuf

#### Быстрый старт с SDK

```csharp
using System;
using System.Threading.Tasks;
using FxidClientSDK.SDK;

// Пример использования
public class Program
{
    static async Task Main(string[] args)
    {
        // Создание экземпляра SDK
        using var sdk = new FxidClientSDK(enableLogging: true);

        try
        {
            // Инициализация со строкой подключения из аргументов командной строки
            // Убедитесь, что передаете --fxid [connectionString] при запуске приложения
            sdk.Initialize();

            // Получение ответа профиля с таймаутом
            var profile = await sdk.GetProfileResponseAsync(TimeSpan.FromSeconds(10));

            if (profile != null)
            {
                Console.WriteLine($"User ID: {profile.User.Fxid}");
                Console.WriteLine($"User Login: {profile.User.Login}");

                // Доступ к различным функциям
                if (profile.Features.Store != null)
                {
                    Console.WriteLine($"Store URL: {profile.Features.Store.Url?.Address}");
                }

                // Проверка активности режима обслуживания
                if (profile.Features.Maintenance?.MaintenanceMode == true)
                {
                    Console.WriteLine($"Maintenance active until: {DateTimeOffset.FromUnixTimeSeconds(profile.Features.Maintenance.EndsAt).ToString()}");
                }
            }
            else
            {
                Console.WriteLine("Failed to get profile response within timeout");
            }

            // Поддержание работы приложения для фоновых обновлений
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

### FXID Server Emulator

Локальный сервер для эмуляции бэкенд-сервисов FXID во время локальной разработки и тестирования.

#### Функции эмулятора
- **JWT-аутентификация**: Генерация и проверка JWT токенов для пользовательских сессий
- **Эмуляция профиля пользователя**: Создание случайных профилей пользователей на основе ID пользователя
- **Интеграция с магазином**: Эмуляция интерфейса магазина с тестовыми продуктами
- **Система объявлений**: Предоставление тестовых объявлений с поддержкой markdown
- **Удаленная конфигурация**: Симуляция функций удаленной конфигурации с тестовыми значениями
- **Локальный HTTP-сервер**: Работает на локальной машине для быстрого тестирования
- **Интеграция с клиентским бинарным файлом**: Может автоматически запускать ваш игровой клиент с соответствующими параметрами

#### Запуск эмулятора

##### Базовое использование
```
dotnet run
```

Это запустит сервер с настройками по умолчанию (порт 5001, ID пользователя 100).

##### Параметры командной строки
```
cd FxidServerEmulator
dotnet run -- [options]
```

Доступные опции:
- `--port <port>`: Установка порта сервера (по умолчанию: 5001)
- `--user-id <id>`: Установка ID пользователя (по умолчанию: 100)
- `--jwt-secret <secret>`: Установка пользовательского JWT секретного ключа
- `--client-binary <path>`: Путь к бинарному файлу игрового клиента
- `--client-args <args>`: Дополнительные аргументы для передачи бинарному файлу клиента
- `--game-server-url <url>`: URL вашего игрового сервера для доставки продуктов (по умолчанию: http://localhost:8080)

## Интеграционное тестирование

Для полной настройки разработки вы можете запустить оба компонента вместе:

1. Запустите Server Emulator:
   ```
   dotnet run -- --port 5001
   ```

2. Настройте ваше клиентское приложение для использования локального эмулятора, установив строку подключения:
   ```
   --fxid "http://localhost:5001/launcher?token=<jwt_token>"
   ```
   Примечание: Server Emulator выведет действительный URL с токеном при запуске.

3. Запустите ваше клиентское приложение с инициализированным SDK.

Эта настройка позволяет тестировать интеграцию вашего клиента с сервисом FXID Profile локально, без необходимости доступа к производственным серверам. Он запустить ваше приложение также как оригинальный лончер запустил бы его. Если интеграция планируется без использования лончера, на пример в мобильной версии приложения, ссылку на апи нужно будет получить с помощью серверной интеграции, но работать она будет аналогично

## Поддержка Protocol Buffer (protobuf)

Апи умеет отвечать на запросы в формате Protocol Buffer (protobuf), так и в json.

В каком формате вы хотите получить ответ от сервера зависит от content-type заголовка запроса.
"application/x-protobuf" - вернет Protocol Buffer (protobuf), любой другой - json


Нативная реализация используется Protocol Buffer (protobuf). Это позволяет эффективно сериализовать данные и уменьшить объем передаваемых данных. Акутальный protobuf находится в папке `FxidServerEmulator/Protos/profile.proto`.

При самостоятельной реализации, можно использовать json

Как Client SDK, так и Server Emulator поддерживают Protocol Buffers для эффективной сериализации данных. Основные типы сообщений:

- `ProfileResponse`: Содержит информацию о пользователе, функциях и сроке действия
- `User`: Базовая идентификация и аутентификация пользователя
- `Features`: Коллекция доступных функций и их конфигураций
  - `LoginFeature`: URL для входа и подключенные провайдеры
  - `StoreFeature`: Доступ к магазину и информация о продуктах
  - `AnnounceFeature`: Объявления и уведомления
  - `TagsFeature`: Теги и атрибуты пользователя
  - `StatFeature`: Конечные точки статистики и аналитики
  - `RemoteConfigFeature`: Значения удаленной конфигурации
  - `MaintenanceFeature`: Информация о графике обслуживания
  - `UpdateFeature`: Информация об обновлении приложения

## Продвинутое использование

### Client SDK: Приостановка и возобновление обновлений

```csharp
// Приостановка обновлений во время интенсивных операций
sdk.PauseFetch();

// Выполнение интенсивной работы...

// Возобновление обновлений, когда готово
sdk.ResumeFetch();
```

### Server Emulator: Настройка ответа профиля

Измените метод `CreateRandomProfileResponseFromUserId` в файле `FxidEmulatorApiHandler.cs`, чтобы настроить данные профиля, возвращаемые клиентам.

## Устранение неполадок

### Проблемы Client SDK
- **Строка подключения не найдена**: Убедитесь, что вы передаете параметр `--fxid` вашему приложению
- **Тайм-аут получения профиля**: Проверьте сетевое подключение и доступность сервера
- **InvalidOperationException**: Убедитесь, что вы вызвали `Initialize()` перед доступом к другим методам

### Проблемы Server Emulator
- **Проблемы с проверкой JWT**: Убедитесь, что ваш клиент использует тот же JWT секретный ключ, что и сервер
- **Отказ в подключении**: Проверьте, что порт не используется другим приложением
- **Отсутствующие статические файлы**: Убедитесь, что ваши статические файлы находятся в правильном месте

## Системные требования

- .NET 6.0 или выше
- C# 9.0 или выше

## Примечание по безопасности

Server Emulator использует фиксированный JWT секретный ключ по умолчанию. Это предназначено только для локального тестирования. Не используйте этот код в производственной среде.

## Публикация на разных платформах


### На мобильном устройстве
Одна интеграция для доступа к глобальным платежным системам, включая Apple, Google и региональных платежных провайдеров.
- App Store
- Google Play
- OneStore
- Xsolla
- Stripe
- bank131
- Youkassa
- И многие другие...

### На ПК
Ваше персональное решение для лаунчера с управлением аккаунтами, эффективной доставкой обновлений и поддержкой P2P CDN.
- Steam
- Epic Games Store
- Собственный лаунчер

# Интеграция с FXID API в Lua (JSON формат)

## Введение

В этом руководстве описывается, как интегрировать приложение на Lua с FXID API, используя JSON формат вместо Protocol Buffer. Это может быть полезно для платформ и языков программирования, где поддержка Protocol Buffer ограничена.

## Предварительные требования

- Lua 5.1 или выше
- Библиотека HTTP-запросов (например, luasocket)
- Библиотека JSON (например, dkjson или cjson)

## Основные шаги интеграции

### 1. Установка необходимых библиотек

```bash
# Установка через LuaRocks
luarocks install luasocket
luarocks install dkjson
```

### 2. Создание класса клиента FXID

```lua
-- fxid_client.lua
local socket = require("socket")
local http = require("socket.http")
local ltn12 = require("ltn12")
local json = require("dkjson")

local FxidClient = {}
FxidClient.__index = FxidClient

function FxidClient.new(enableLogging)
    local self = setmetatable({}, FxidClient)
    self.connectionString = nil
    self.latestProfileResponse = nil
    self.isPaused = false
    self.enableLogging = enableLogging or false
    self.timer = nil
    return self
end

function FxidClient:log(message)
    if self.enableLogging then
        print("[FXID] " .. message)
    end
end

function FxidClient:initialize()
    -- Получение строки подключения из аргументов запуска
    local args = arg or {}
    local fxidIndex = nil

    for i, v in ipairs(args) do
        if v == "--fxid" and i < #args then
            fxidIndex = i
            break
        end
    end

    if fxidIndex then
        self.connectionString = args[fxidIndex + 1]
        self:log("Connection string initialized: " .. self.connectionString)
    else
        error("Connection string not found. Please provide it using the --fxid flag.")
    end

    self:connect()
end

function FxidClient:connect()
    if not self.connectionString then
        error("Connection string is not initialized. Call initialize() first.")
    end

    -- Отмена существующих таймеров
    if self.timer then
        self:disconnect()
    end

    -- Запуск первого обновления
    self:fetchUpdate()

    self:log("Connected and started fetching updates.")
end

function FxidClient:fetchUpdate()
    if self.isPaused then
        -- Проверка каждую секунду, если приостановлено
        self.timer = socket.select(nil, nil, 1)
        self:fetchUpdate()
        return
    end

    self:log("Attempting to fetch update from: " .. self.connectionString)

    local response_body = {}
    local result, status_code, headers = http.request {
        url = self.connectionString,
        method = "GET",
        headers = {
            ["Accept"] = "application/json"  -- Запрашиваем ответ в формате JSON
        },
        sink = ltn12.sink.table(response_body)
    }

    if result and status_code == 200 then
        local response_text = table.concat(response_body)
        self:log("Received response of length: " .. #response_text)

        local success, profile_response = pcall(json.decode, response_text)

        if success and profile_response then
            self:log("Successfully parsed profile response")

            -- Обновление последнего ответа профиля
            self.latestProfileResponse = profile_response

            -- Расчет задержки до следующего обновления
            local current_time = os.time()
            local expiration_time = profile_response.expirationTimestamp or (current_time + 300)
            local delay_until_next_fetch = (expiration_time - current_time)
            delay_until_next_fetch = math.max(delay_until_next_fetch, 10) -- минимум 10 секунд между запросами

            -- Обновление строки подключения для следующего запроса
            if profile_response.refreshUrl and profile_response.refreshUrl.address then
                self.connectionString = profile_response.refreshUrl.address
                self:log("Updated connection string to: " .. self.connectionString)
            end

            self:log("Waiting " .. delay_until_next_fetch .. "s before next fetch")

            -- Планирование следующего обновления
            self.timer = socket.select(nil, nil, delay_until_next_fetch)
            self:fetchUpdate()
        else
            self:log("Error parsing profile response: " .. (profile_response or "unknown error"))
            -- Повтор через 10 секунд в случае ошибки
            self.timer = socket.select(nil, nil, 10)
            self:fetchUpdate()
        end
    else
        self:log("HTTP request error: " .. (status_code or "unknown"))
        -- Повтор через 10 секунд в случае ошибки
        self.timer = socket.select(nil, nil, 10)
        self:fetchUpdate()
    end
end

function FxidClient:disconnect()
    self.timer = nil
    self:log("Disconnected and stopped fetching updates.")
end

function FxidClient:pauseFetch()
    if not self.isPaused then
        self.isPaused = true
        self:log("Background fetch paused.")
    end
end

function FxidClient:resumeFetch()
    if self.isPaused then
        self.isPaused = false
        self:log("Background fetch resumed.")
        self:fetchUpdate()
    end
end

function FxidClient:getProfileResponse()
    return self.latestProfileResponse
end

return FxidClient
```

### 3. Пример использования клиента

```lua
-- main.lua
local FxidClient = require("fxid_client")

-- Создание экземпляра клиента с включенным логированием
local client = FxidClient.new(true)

-- Обработка ошибок
local status, err = pcall(function()
    -- Инициализация клиента
    -- Убедитесь, что вы передаете --fxid [connectionString] при запуске приложения
    client:initialize()

    -- Ожидание первого получения профиля
    local start_time = os.time()
    local timeout = 10 -- 10 секунд таймаут

    while os.time() - start_time < timeout do
        local profile = client:getProfileResponse()
        if profile then
            print("User ID: " .. (profile.user and profile.user.fxid or "unknown"))
            print("User Login: " .. (profile.user and profile.user.login or "unknown"))

            -- Доступ к различным функциям
            if profile.features and profile.features.store then
                print("Store URL: " .. (profile.features.store.url and profile.features.store.url.address or "unknown"))
            end

            -- Проверка активности режима обслуживания
            if profile.features and profile.features.maintenance and profile.features.maintenance.maintenanceMode then
                local ends_at = profile.features.maintenance.endsAt or 0
                print("Maintenance active until: " .. os.date("%Y-%m-%d %H:%M:%S", ends_at))
            end

            break
        end

        socket.sleep(1)
    end

    if not client:getProfileResponse() then
        print("Failed to get profile response within timeout")
    end

    -- Поддержание работы приложения для фоновых обновлений
    print("Press Ctrl+C to exit...")
    while true do
        socket.sleep(1)
    end
end)

if not status then
    print("Error: " .. err)
end

-- Отключение клиента перед выходом
client:disconnect()
```

## Структура ответа JSON

В отличие от Protocol Buffer, при использовании формата JSON структура ответа будет выглядеть примерно так:

```json
{
  "user": {
    "fxid": "user123",
    "login": "player@example.com"
  },
  "features": {
    "login": {
      "url": {
        "address": "https://login.example.com"
      },
      "providers": ["email", "google", "facebook"]
    },
    "store": {
      "url": {
        "address": "https://store.example.com"
      },
      "products": [...]
    },
    "announce": {
      "announcements": [...]
    },
    "tags": {
      "tags": [...]
    },
    "stat": {
      "endpoint": "https://stats.example.com"
    },
    "remoteConfig": {
      "values": {...}
    },
    "maintenance": {
      "maintenanceMode": false,
      "endsAt": 0
    },
    "update": {
      "version": "1.0.0",
      "mandatory": false,
      "url": {
        "address": "https://update.example.com"
      }
    }
  },
  "expirationTimestamp": 1676554800,
  "refreshUrl": {
    "address": "https://api.example.com/profile/refresh?token=xyz"
  }
}
```

## Примечания по интеграции

### Отличия от Protocol Buffer

1. **Имена полей**: В JSON имена полей используются в camelCase, в отличие от snake_case в .proto файлах
2. **Структура вложенных объектов**: JSON использует вложенность объектов напрямую
3. **Эффективность**: JSON менее эффективен с точки зрения размера данных, но более прост для отладки

### Обработка ошибок

При работе с JSON API важно обрабатывать ошибки парсинга, так как структура ответа может меняться:

```lua
local success, parsed_data = pcall(json.decode, response_text)
if not success then
    -- Обработка ошибки парсинга JSON
    print("Error parsing JSON: " .. parsed_data)
    return
end
```

### Тестирование с эмулятором

Для тестирования с FXID Server Emulator, удостоверьтесь, что запрашиваете данные в формате JSON:

```lua
local result, status_code, headers = http.request {
    url = "http://localhost:5001/launcher?token=<jwt_token>",
    method = "GET",
    headers = {
        ["Accept"] = "application/json"  -- Явно запрашиваем JSON
    },
    sink = ltn12.sink.table(response_body)
}
```

## Дополнительные возможности

### Асинхронные запросы

Для игр и приложений, требующих неблокирующих операций ввода-вывода, рекомендуется использовать асинхронные библиотеки HTTP, такие как lua-http или copas:

```bash
luarocks install http
# или
luarocks install copas
```

### Кэширование профиля

Для улучшения производительности можно реализовать кэширование профиля пользователя:

```lua
function FxidClient:saveProfileToCache(profile)
    local file = io.open("profile_cache.json", "w")
    if file then
        file:write(json.encode(profile))
        file:close()
    end
end

function FxidClient:loadProfileFromCache()
    local file = io.open("profile_cache.json", "r")
    if file then
        local content = file:read("*all")
        file:close()
        local success, profile = pcall(json.decode, content)
        if success then
            return profile
        end
    end
    return nil
end
```

## Заключение

Эта интеграция демонстрирует, как можно использовать FXID API с JSON в приложениях на Lua. Такой подход может быть адаптирован для других языков и платформ, где Protocol Buffer не является оптимальным выбором.
