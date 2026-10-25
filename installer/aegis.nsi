!include "MUI2.nsh"
Unicode true
Name "Aegis Bêta"
!ifndef PACKAGE_DIR
 !define PACKAGE_DIR "..\artifacts\release"
!endif
!ifndef APP_VERSION
 !define APP_VERSION "0.4.0-beta.1"
!endif
OutFile "..\artifacts\Aegis-${APP_VERSION}-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\Aegis"
RequestExecutionLevel user
SetCompressor zlib
!define MUI_ICON "..\src\Aegis.Desktop\Assets\aegis.ico"
!define MUI_UNICON "..\src\Aegis.Desktop\Assets\aegis.ico"
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "French"

Function .onInit
 StrCpy $INSTDIR "$LOCALAPPDATA\Programs\Aegis"
 IfFileExists "$INSTDIR\*" 0 ready
 System::Call 'kernel32::CreateFileW(w "$INSTDIR\.aegis-running.lock", i 0xC0000000, i 0, p 0, i 4, i 0x80, p 0) p.r0'
 StrCmp $0 -1 running
 System::Call 'kernel32::CloseHandle(p r0)'
 Goto ready
 running:
 IfSilent 0 +3
 SetErrorLevel 2
 Abort
 MessageBox MB_ICONSTOP "Fermez Aegis avant de le mettre à jour. Vos sessions seront conservées."
 Abort
 ready:
FunctionEnd

Function un.onInit
 StrCmp $INSTDIR "$LOCALAPPDATA\Programs\Aegis" safe
 MessageBox MB_ICONSTOP "Le dossier de désinstallation ne correspond pas au dossier Aegis attendu."
 Abort
 safe:
 System::Call 'kernel32::CreateFileW(w "$INSTDIR\.aegis-running.lock", i 0xC0000000, i 0, p 0, i 4, i 0x80, p 0) p.r0'
 StrCmp $0 -1 running
 System::Call 'kernel32::CloseHandle(p r0)'
 Goto ready
 running:
 IfSilent 0 +3
 SetErrorLevel 2
 Abort
 MessageBox MB_ICONSTOP "Fermez Aegis avant de le désinstaller. Vos sessions seront conservées."
 Abort
 ready:
FunctionEnd

Section "Aegis"
 SetOutPath "$INSTDIR"
 Delete "$INSTDIR\NAudio*.dll"
 Delete "$INSTDIR\System.Speech.dll"
 RMDir /r "$INSTDIR\tools\ollama"
 RMDir /r "$INSTDIR\tools\whisper"
 File /r /x models /x *.pdb /x .staging /x *.tmp /x .aegis-running.lock "${PACKAGE_DIR}\*"
 WriteUninstaller "$INSTDIR\Uninstall.exe"
 CreateDirectory "$SMPROGRAMS\Aegis"
 CreateShortcut "$SMPROGRAMS\Aegis\Aegis.lnk" "$INSTDIR\Aegis.exe"
 CreateShortcut "$DESKTOP\Aegis.lnk" "$INSTDIR\Aegis.exe"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis" "DisplayName" "Aegis (Bêta)"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis" "UninstallString" '"$INSTDIR\Uninstall.exe"'
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis" "DisplayVersion" "${APP_VERSION}"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis" "Publisher" "Aegis contributors"
 WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis" "DisplayIcon" "$INSTDIR\Aegis.exe"
SectionEnd

Section "Uninstall"
 ClearErrors
 RMDir /r "$INSTDIR"
 IfErrors failed
 Delete "$DESKTOP\Aegis.lnk"
 Delete "$SMPROGRAMS\Aegis\Aegis.lnk"
 RMDir "$SMPROGRAMS\Aegis"
 ReadRegStr $0 HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "AegisPCMonitor"
 StrCmp $0 '"$INSTDIR\Aegis.exe" --background' 0 +2
 DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "AegisPCMonitor"
 DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Aegis"
 ; Suppression des fichiers installés et des éventuels composants téléchargés dans le dossier d'application
 Goto done
 failed:
 MessageBox MB_ICONEXCLAMATION "Certains fichiers sont encore utilisés. Fermez Aegis et relancez la désinstallation."
 done:
 ; Les sessions utilisateur dans %LOCALAPPDATA%\AegisNext sont préservées pour ne pas perdre les mesures enregistrées.
SectionEnd
