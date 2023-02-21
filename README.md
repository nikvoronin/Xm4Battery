# WMI for Plug-and-Play Devices

> WMI = Windows Management Interface.

The primary goal is to get battery level of WH-1000XM4 headphones.

- WMI
- Win32_PnPEntity, PNPDeviceID
- GetDeviceProperties
- {104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2
- ManagementBaseObject, ManagementObject, ManagementObjectCollection

## Specific Device Properties

> Key = {GUID} pid

### Is Connected

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 15`
- Type = 17 (Boolean)

Data = False → device is disconnected

```plain
Data : False
DeviceID : BTHENUM\DEV_F84E17FE9B55\8&37022D96&0&BLUETOOTHDEVICE_F84E17FE9B55
key : {83DA6326-97A6-4088-9453-A1923F573B29} 15
KeyName : --same as the key above--
Type : 17 (Boolean)
```

### Last Arrival Date

- Key = `{83DA6326-97A6-4088-9453-A1923F573B29} 102`
- KeyName = DEVPKEY_Device_LastArrivalDate
- Type = 16 (FileTime)

Data = 20230131090906.098359+180 → 2023 Jan 31, 9:09:06 GMT+3

```plain
Data : 20230131090906.098359+180
DeviceID : BTHHFENUM\BTHHFPAUDIO\9&2DBCFC8&0&97
key : {83DA6326-97A6-4088-9453-A1923F573B29} 102
KeyName : DEVPKEY_Device_LastArrivalDate
Type : 16 (FileTime)
```

### Last Removal Date

Key = {83da6326-97a6-4088-9453-a1923f573b29} 103

## Biblio

- [Enumerating windows device](https://www.codeproject.com/articles/14412/enumerating-windows-device). Enumerating the device using the SetupDi* API provided with WinXP. CodeProject // 17 Jun 2006
- [How to get the details for each enumerated device?](https://social.msdn.microsoft.com/Forums/en-US/65086709-cee8-4efa-a794-b32979abb0ea/how-to-get-the-details-for-each-enumerated-device?forum=vbgeneral) MSDN, Archived Forums 421-440, Visual Basic.
