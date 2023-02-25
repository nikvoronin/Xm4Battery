﻿using LanguageExt;
using static LanguageExt.Compositions<A>;

namespace WmiPnp.Xm4
{
    public class Xm4Entity
    {
        private PnpEntity _xm4;

        private Xm4Entity( PnpEntity xm4 )
        { 
            _xm4 = xm4;
        }

        public static Option<Xm4Entity> Create()
            => PnpEntity
            .ByFriendlyName( PnpEntity_FriendlyName )
            .Some( xm4 => (Option<Xm4Entity>) new Xm4Entity( xm4 ) )
            .None( Option<Xm4Entity>.None );

        const string PnpEntity_FriendlyName = "WH-1000XM4 Hands-Free AG";
        const string DeviceProperty_BatteryLevelKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
        const string DeviceProperty_IsConnected = "{83DA6326-97A6-4088-9453-A1923F573B29} 15";
        
        const string DEVPKEY_Bluetooth_LastConnectedTime = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 11";
        const string DEVPKEY_Bluetooth_LastConnectedTime2 = "{2BD67D8B-8BEB-48D5-87E0-6CDA3428040A} 5";
    }
}