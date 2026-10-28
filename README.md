# ПиОГИ — лабораторные работы

Учебные проекты на C# и WPF. Каждая лабораторная находится в отдельной папке, история первой работы сохранена в этом репозитории.

## Структура

```text
Lab1.EnemyEditor/              Редактор шаблонов противников (лаба 1)
  laba 1.sln                  Решение для Visual Studio
  laba 1/                     Исходники WPF-проекта
Lab2.Clicker/                 Игра-кликер (лаба 2)
  Clicker.sln                 Решение для Visual Studio
  Clicker.App/                WPF-приложение кликера
  Clicker.Core/               BigNumber и будущая игровая логика
  Clicker.Checks/             Проверки игровой логики без сторонних библиотек
Lab3.TimedClicker/            Кликер на время и бонусные сферы (лаба 3)
  TimedClicker.sln            Решение мини-игры
  TimedClicker.App/           WPF-интерфейс
  TimedClicker.Core/          Контроллер, абстрактные сферы и перезарядка
  TimedClicker.Checks/        Консольные проверки
assets/
  icons/                      Иконки противников
  templates/
    enemies.sample.json       Пример шаблонов из первой лабы
```

## Лаба 1: редактор противников

Реализованы загрузка иконок, добавление и удаление шаблонов, сохранение и загрузка JSON.

Для запуска нужны Windows, .NET 8 SDK или более новый совместимый SDK, а также поддержка WPF. В Visual Studio должен быть установлен компонент «Разработка классических приложений .NET».

1. Откройте `Lab1.EnemyEditor/laba 1.sln` в Visual Studio.
2. Запустите проект клавишей F5.
3. В редакторе выберите папку `assets/icons` из корня репозитория.
4. Загрузите `assets/templates/enemies.sample.json` или создайте свои шаблоны.

Запуск из PowerShell в корне репозитория:

```powershell
dotnet run --project "Lab1.EnemyEditor/laba 1/laba 1.csproj"
```

JSON хранит имя файла иконки, поэтому соответствующее изображение должно находиться в выбранной папке иконок.

## Лаба 2: кликер и инкапсуляция

По текущему условию это лабораторная №2. Реализованы собственный неизменяемый `BigNumber` с четырьмя поразрядными операциями, классы `Player` и `Enemy`, загрузка JSON, нормализация шансов, выбор и масштабирование противников. `GameSession` связывает бой, награду и улучшения, а `Clicker.App` предоставляет WPF-интерфейс.

Для запуска в Visual Studio откройте `Lab2.Clicker/Clicker.sln`, назначьте `Clicker.App` запускаемым проектом и нажмите F5. Из PowerShell в корне репозитория:

```powershell
dotnet run --project "Lab2.Clicker/Clicker.App/Clicker.App.csproj"
```

Пример JSON и иконки загружаются автоматически. Другой JSON и папку иконок можно выбрать в окне. Подробности и сценарий проверки перед сдачей — в `Lab2.Clicker/README.md`.

Проверки запускаются из корня репозитория:

```powershell
dotnet run --project "Lab2.Clicker/Clicker.Checks/Clicker.Checks.csproj"
```

## Лаба 3: абстракция, наследование, полиморфизм

Задания 1–2: самостоятельная мини-игра в `Lab3.TimedClicker/TimedClicker.sln`. Задание 3: перезарядка атак, её улучшение за золото и бонусные сферы добавлены в существующий `Lab2.Clicker`. Общие `Collectable` и `Cooldown` повторно используются через ссылку на `TimedClicker.Core`.

```powershell
dotnet run --project Lab3.TimedClicker/TimedClicker.App
dotnet run --project Lab3.TimedClicker/TimedClicker.Checks
```

Правила бонусов, структура классов, объяснения для защиты и сценарии ручной проверки — в [README лабы 3](Lab3.TimedClicker/README.md). WPF-окна запускаются на Windows; консольные проверки работают также на macOS/Linux.

## Работа с Git

Общие изменения структуры и каждой лабораторной фиксируются отдельными коммитами. Временные файлы Visual Studio, `bin` и `obj` исключены через `.gitignore`.

Перед коммитом проверьте изменения:

```powershell
git status
git diff
```
