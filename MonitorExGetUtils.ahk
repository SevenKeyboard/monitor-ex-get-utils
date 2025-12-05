#Requires AutoHotkey v1.1.17+
#Include %A_ScriptDir%
#Include .\lib\DisplayMonitorManager.ahk
;=============================================================
; MonitorExGetUtils — Extended monitor information helpers
;
; GitHub: https://github.com/SevenKeyboard/monitor-ex-get-utils
; Author: SevenKeyboard Ltd. (2025)
; License: MIT License
;=============================================================
class VersionManager_MonitorExGetUtils
{
    static _ := VersionManager_MonitorExGetUtils._init()
    _init()    {
        global
        MONITOREXGETUTILS_VERSION := "1.0.0"
        if (!this._verCheck(DISPLAYMONITORMANAGER_VERSION, "1.0.0"))
            throw exception("DisplayMonitorManager version 1.x is required (minimum 1.0.0).")
        return true
    }
    _verCheck(byRef actual, required)    {
        local
        if !isSet(actual)
            return false
        actualMajor     := strSplit(actual, ".",, 2)[1]
        requiredMajor   := strSplit(required, ".",, 2)[1]
        if (actualMajor != requiredMajor)
            return false
        return verCompare(actual, ">=" required)
    }
}
;---------------------------------------------
monitorExGetInfoList(force:=false)    {
    return DisplayMonitorManager.getList(force)
}
monitorExGetInfo(N:="", force:=false)    {
    if (N=="")    {
        N:=1
        for i,info in monitorExGetInfoList()    {
            if (info.dwFlags)    {
                N:=i
                break
            }
        }
    }
    list:=DisplayMonitorManager.getList(force), ErrorLevel:=list.hasKey(N)?0:1
    return (list.hasKey(N)?list[N]:"")
}
;---------------------------------------------
monitorExGetScaleFactor(N:="", default:=100, force:=false)    {
    static S_OK:=0x00000000
    for i,info in monitorExGetInfoList(force)    {
        switch (N)
        {
            case "":
                if (!info.dwFlags)
                    continue
            default:
                if (i!==N)
                    continue
        }
        return (dllCall("Shcore.dll\GetScaleFactorForMonitor", "Ptr",info.hMonitor, "Int*",scale, "Ptr")==S_OK)
            ?scale
            :default
    }
    return default
}
;---------------------------------------------
monitorExGet(N:="", byRef left:="", byRef top:="", byRef right:="", byRef bottom:="", force:=false)    {
    return _monitorExGetRect_583C39F6("rcMonitor", N, left, top, right, bottom, force)
}
monitorExGetWorkArea(N:="", byRef left:="", byRef top:="", byRef right:="", byRef bottom:="", force:=false)    {
    return _monitorExGetRect_583C39F6("rcWork", N, left, top, right, bottom, force)
}
_monitorExGetRect_583C39F6(propertyName, N:="", byRef left:="", byRef top:="", byRef right:="", byRef bottom:="", force:=false)    {
    if (N=="")    {
        N:=1
        for i,info in monitorExGetInfoList()    {
            if (info.dwFlags)    {
                N:=i
                break
            }
        }
    }
    list:=DisplayMonitorManager.getList(force)
    if (list.hasKey(N))    {
        left    := list[N][propertyName].left
        ,top    := list[N][propertyName].top
        ,right  := list[N][propertyName].right
        ,bottom := list[N][propertyName].bottom
        ,ErrorLevel:=0
        return N
    }  else  {
        left:= top:= right:= bottom:= "", ErrorLevel:=1
        return 0
    }
}
monitorExGetName(N:="", force:=false)    {
    info:=monitorExGetInfo(N, force)
    return !ErrorLevel
        ?info.szDevice
        :""
}
monitorExGetPrimary(force:=false)    {
    for i,info in monitorExGetInfoList(force)    {
        if (info.dwFlags)
            return i
    }
    return 0
}
monitorExGetCount(force:=false)    {
    return monitorExGetInfoList(force).length()
}