#Requires AutoHotkey v2.0.0+
#Include "%A_ScriptDir%"
#Include ".\lib\DisplayMonitorInfoManager.ahk"
;=============================================================
; MonitorExGetUtils — Extended monitor information helpers
;
; GitHub: https://github.com/SevenKeyboard/monitor-ex-get-utils
; Author: SevenKeyboard Ltd. (2025)
; License: MIT License
;=============================================================
class VersionManager_MonitorExGetUtils
{
    static _ := this._init()
    static _init()    {
        global
        MONITOREXGETUTILS_VERSION := "1.0.0"
        if (!this._verCheck(&DISPLAYMONITORINFOMANAGER_VERSION, "1.0.0"))
            throw valueError("DisplayMonitorInfoManager version 1.x is required (minimum 1.0.0).")
        return true
    }
    static _verCheck(&actual, required)    {
        if !isSet(actual)
            return false
        actualMajor     := strSplit(actual, ".",, 2)[1]
        requiredMajor   := strSplit(required, ".",, 2)[1]
        if (actualMajor != requiredMajor)
            return false
        return verCompare(actual, ">=" required)
    }
}
monitorExGetInfoList(force:=false)    {
    return DisplayMonitorInfoManager.getList(force)
}
monitorExGetInfo(N?, force:=false)    {
    if (!isSet(N))    {
        N:=1
        for i,info in monitorExGetInfoList()    {
            if (info.dwFlags)    {
                N:=i
                break
            }
        }
    }
    list:=DisplayMonitorInfoManager.getList(force)
    if (list.has(N))
        return list[N]
    throw valueError("Parameter #1 invalid", -2, N)
}
;---------------------------------------------
monitorExGetScaleFactor(N?, default:=100, force:=false)    {
    static S_OK:=0x00000000
    for i,info in monitorExGetInfoList(force)    {
        switch (isSet(N))
        {
            default:
                if (!info.dwFlags)
                    continue
            case true:
                if (i!==N)
                    continue
        }
        return (dllCall("Shcore.dll\GetScaleFactorForMonitor", "Ptr",info.hMonitor, "Int*",&scale:=0, "Ptr")==S_OK)
            ?scale
            :default
    }
    return default
}
;---------------------------------------------
monitorExGet(N?, &left?, &top?, &right?, &bottom?, force:=false)    {
    return _monitorExGetRect_583C39F6("rcMonitor", N??unset, &left, &top, &right, &bottom, force)
}
monitorGetWorkArea(N?, &left?, &top?, &right?, &bottom?, force:=false)    {
    return _monitorExGetRect_583C39F6("rcWork", N??unset, &left, &top, &right, &bottom, force)
}
_monitorExGetRect_583C39F6(propertyName, N?, &left?, &top?, &right?, &bottom?, force:=false)    {
    if (!isSet(N))    {
        N:=1
        for i,info in monitorExGetInfoList()    {
            if (info.dwFlags)    {
                N:=i
                break
            }
        }
    }
    list:=DisplayMonitorInfoManager.getList(force)
    if (list.has(N))    {
        left    := list[N].%propertyName%.left
        ,top    := list[N].%propertyName%.top
        ,right  := list[N].%propertyName%.right
        ,bottom := list[N].%propertyName%.bottom
        return N
    }  else  {
        throw valueError(propertyName=="rcMonitor"
            ?"Parameter #1 of MonitorExGet is invalid"
            :"Parameter #1 of MonitorGetWorkArea is invalid"
            ,-2, N)
    }
}
monitorExGetName(N?, force:=false)    {
    try  {
        info:=monitorExGetInfo(N??unset, force)
        return info.szDevice
    }  catch  {
        throw valueError("Parameter #1 of MonitorExGetName is invalid", -2, N)
    }
}
monitorExGetPrimary(force:=false)    {
    for i,info in monitorExGetInfoList(force)    {
        if (info.dwFlags)
            return i
    }
    return 0
}
monitorExGetCount(force:=false)    {
    return monitorExGetInfoList(force).Length
}