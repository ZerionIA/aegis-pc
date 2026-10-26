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
!define MUI_COMPONENTSPAGE_TEXT_TOP "Aegis contient son runtime. Les capteurs CPU complets utilisent le pilote officiel signé PawnIO et demandent une confirmation administrateur Windows. Décochez cette option pour le mode basique."
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\Aegis.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Lancer Aegis"
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

Section "Aegis (application et runtime)" SecAegis
 SectionIn RO
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
 ExecWait '"$INSTDIR\Aegis.exe" --configure-basic-if-new' $0
 StrCmp $0 0 initial_config_done
 IfSilent +2
 MessageBox MB_ICONSTOP "Impossible d’enregistrer la configuration utilisateur. Vérifiez les droits et l’espace disque, puis relancez l’installation."
 SetErrorLevel 3
 Abort
 initial_config_done:
SectionEnd

Section "Capteurs CPU complets (pilote PawnIO)" SecHardware
 ; A silent update never silently adds a system driver. Setup remains available in the app.
 IfSilent hardware_done
 DetailPrint "Configuration des capteurs CPU : pilote signé, puis confirmation Windows pour la lecture."
 ExecWait '"$INSTDIR\Aegis.exe" --install-hardware' $0
 StrCmp $0 0 hardware_done
 StrCmp $0 3010 hardware_reboot
 MessageBox MB_ICONINFORMATION "Le prérequis matériel n’a pas été installé ou a été refusé. Aegis reste utilisable en mode basique. Vous pouvez reprendre la configuration depuis Réglages → Capteurs CPU."
 Goto hardware_done
 hardware_reboot:
 MessageBox MB_ICONINFORMATION "PawnIO est installé. Windows demande un redémarrage du PC avant la lecture complète des capteurs CPU."
 hardware_done:
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
 !insertmacro MUI_DESCRIPTION_TEXT ${SecAegis} "Monitoring, HUD, sessions, exports et runtime .NET inclus."
 !insertmacro MUI_DESCRIPTION_TEXT ${SecHardware} "Version officielle signée de PawnIO incluse. Autorisation administrateur à l’installation du pilote et au lancement du collecteur complet. Sans cette option, certaines mesures CPU restent indisponibles."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

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
