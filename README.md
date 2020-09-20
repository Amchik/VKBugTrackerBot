~~_боже мой, я наконец то написал описание_~~

_удивительно, обновил_

# VKBugTrackerBot

Бот, который парсит багтрекер ВК и пишет об этом тебе в лс

## Запуск

Собрать его можно командой `dotnet build`. Для докера `docker build -t "name:tag" .`

При первом запуске сгенерируется `config.json` (`/app/config.json` для докера):
```json
{
    "remixsid": "see_readme",
    "access-token": "see_readme",
    "group-id": 123,
    "admins": [
        123,
        456
    ]
}
```

`remixsid` - это куки, отправляемые ВКонтактом для авторизации. Они нужны для получения последних отчётов, так как я не нашёл нормального API для их получения. В Chrome нахождение куки интуетивно понятно, в Firefox я этого не нашёл. Впринципе через Cookie Quick Manager это значение можно достать.
Желательно открыть приватную вкладку, зайти в ВК и взять новый `remixsid`, иначе можете вылететь при его смене или бот крашнется.

Для получения ключа доступа (`access-token`) зайдите в группу, где у вас есть права администратора, выберете пункт _Работа с API_->_Ключи доступа_ и сгенерируйте новый с правами на доступ к сообщениям.
Также там же зайдите в раздел _Long Poll API_, включите его, поставьте последнию версию (ну например 5.103) и в _Типы событий_ поставте галочку напротив входящих сообщений и по желанию на против редактированию сообщений.

![](https://sun3-10.userapi.com/wUQKtd-oUKBpx3y_XvMgAIjkXuR0l8urb8oMew/T26M3Cb5OSM.jpg "Молодец, если у тебя тёмная тема гитхаба :)")
![](https://sun3-13.userapi.com/nswWvrk4_vQIGijD8kqI0FwnE2HjBR7CgXAawg/hMFC0Zvcodc.jpg)
![](https://sun3-13.userapi.com/CW1fTiNujTaf20_Fgiti1s0Uf0ODvbYyizBn4A/sRpb6kILDjc.jpg)

Самый лёгкий способ получить `group-id` (ну если же ты изменял короткую ссылку на группу) это зайти в статистику и посмотреть на параметр `gid` в url.

`admins` - это администраторы, которые могут использовать соответсвующие команды. Там нужно писать их idшники. Я впринципе всегда через фотографию получаю его...

## Команды
#### Пользовательские
* `/toggleAll` - включает/выключает все сообщения
* `/toggleNotifications` - включает/выключает сообщения, отправленные командой `/send`
* `/toggleProduct <prodict name>` - включает/выключает отображение репортов из продукта `<product name>` 
* `/status` - выводит персональные настройки
* `/bookmarks [page]` - выводит закладки
* `/help` - выводит помощь по командам

#### Административные
* `/send <message>` - отправляет сообщение всем, кто не выключил это
* `/admin <user ID>` - даёт/отбирает права администратора пользователю (см. примечание 1)

## Примечания

1. Использование: `/admin 1`. Не работает: `/admin Вова`, `/admin @id1`, `/admin @durov`, `/admin id1` и т.д.
