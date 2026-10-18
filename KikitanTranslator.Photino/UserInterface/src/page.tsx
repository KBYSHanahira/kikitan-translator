import * as React from 'react';

import Kikitan from "./pages/Kikitan"

import {
    Button,
    IconButton,
    CircularProgress,
    Tooltip,
} from '@mui/material';
import { ThemeProvider } from '@mui/material/styles';

import {
    Settings,
    Translate,
    WbSunny,
    NightsStay,
    DownloadOutlined,
} from '@mui/icons-material';

import Changelogs from './pages/Changelogs';
import {localization} from './util/localization';
import {
    controlKikitan,
    registerStateCallback,
    sendAppState,
    setConfig,
    update
} from "./util/photino.ts";
import QuickstartMenu from "./components/Quickstart.tsx";
import SettingsPage from "./pages/Settings.tsx";
import Modal from "./components/Modal.tsx";
import {app_state} from "./util/constants.ts";
import {buildTheme} from "./util/theme.ts";

let updateViewed = false;

function App() {
    const [changelogsVisible, setChangelogsVisible] = React.useState(false)
    const [settingsVisible, setSettingsVisible] = React.useState(false)
    const [updaterVisible, setUpdaterVisible] = React.useState(false)
    const [updating, setUpdating] = React.useState(false)
    const [showProgress, setShowProgress] = React.useState(false)
    const [updaterText, setUpdaterText] = React.useState("")
    const [newVersion, setNewVersion] = React.useState("")
    const [appVersion, setAppVersion] = React.useState("")

    const [stateUpdated, setStateUpdated] = React.useState<boolean>(false)
    // @ts-ignore
    const [appState, setAppState] = React.useState<app_state>({});

    const [loaded, setLoaded] = React.useState(false)

    const lightMode = appState.config?.light_mode ?? false
    const theme = React.useMemo(() => buildTheme(lightMode), [lightMode])

    React.useEffect(() => {
        document.documentElement.dataset.theme = lightMode ? "light" : "dark"
    }, [lightMode])

    React.useEffect(() => {
        setTimeout(() => setLoaded(true), 300);

        sendAppState()
        registerStateCallback(state => {
            if (!updateViewed) {
                setUpdaterText(localization.new_update_available[state.config.language])
                setUpdaterVisible(state.server_version != null)

                setAppVersion(state.app_version)
                setNewVersion(state.server_version)
            }

            setChangelogsVisible(state.config.last_version != state.app_version)

            setAppState(state)
            setStateUpdated(!stateUpdated)
        })
    }, [])

    if (appState.config == undefined) return null

    const lang = appState.config.language
    const linuxNoAppImage = appState.is_linux && !appState.is_appimage

    return (
        <ThemeProvider theme={theme}>
            <div
                className={`relative h-screen flex flex-col bg-bg text-ink select-none transition-opacity duration-500 ${!loaded ? "opacity-0 pointer-events-none" : "opacity-100"}`}>

                <Modal z="z-20" open={!appState.config.quickstart_viewed && appState.config.language != null}
                       className="w-[min(860px,92vw)] h-[min(560px,88vh)]">
                    <QuickstartMenu state={appState}/>
                </Modal>

                <Modal open={updaterVisible && appState.config.quickstart_viewed} className="w-[min(440px,90vw)] p-6 gap-5">
                    {!linuxNoAppImage ? <>
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 shrink-0 rounded-control bg-accent text-accent-ink grid place-items-center">
                                {showProgress ? <CircularProgress size={22}/> : <DownloadOutlined/>}
                            </div>
                            <div className="flex flex-col min-w-0">
                                <p className="text-lg font-semibold">{updaterText}</p>
                                {!showProgress && <p className="text-sm text-muted">v{appVersion} → <span className="text-accent-text font-semibold">{newVersion}</span></p>}
                            </div>
                        </div>
                        {!showProgress &&
                            <div className="flex justify-end gap-2">
                                <Button variant="text" color="inherit" disabled={updating} onClick={() => {
                                    updateViewed = true;
                                    setUpdaterVisible(false)
                                }}>{localization.close[lang]}</Button>
                                <Button variant="contained" disabled={updating} startIcon={<DownloadOutlined/>} onClick={() => {
                                    setUpdating(true)
                                    setTimeout(() => {
                                        setShowProgress(true)
                                        setUpdaterText(localization.updating[lang])
                                        update()
                                    }, 300)
                                }}>{localization.update[lang]}</Button>
                            </div>
                        }
                    </> : <>
                        <p className="text-lg font-semibold">{updaterText}</p>
                        <p className="text-sm text-muted">{localization.not_appimage_warning[lang]}</p>
                        <div className="flex justify-end">
                            <Button variant="outlined" onClick={() => {
                                updateViewed = true;
                                setUpdaterVisible(false)
                            }}>{localization.close[lang]}</Button>
                        </div>
                    </>}
                </Modal>

                <Modal open={settingsVisible} className="w-[min(880px,92vw)] h-[min(560px,88vh)]">
                    <SettingsPage state={appState} closeCallback={() => {
                        setSettingsVisible(false)
                        controlKikitan(true)
                    }}/>
                </Modal>

                {appState.config.quickstart_viewed && changelogsVisible &&
                    <Modal open={changelogsVisible} className="w-[min(760px,92vw)] h-[min(520px,85vh)]">
                        <Changelogs state={appState} closeCallback={() => {
                            setConfig("last_version", appState.app_version)
                            setChangelogsVisible(false)
                        }}/>
                    </Modal>
                }

                <header className="h-14 shrink-0 flex items-center gap-4 px-5 border-b border-line bg-surface">
                    <div className="flex items-center gap-2.5 min-w-0">
                        <span className="w-3 h-3 rounded-[3px] bg-accent shrink-0" aria-hidden="true"/>
                        <span className="text-md font-extrabold tracking-tight">Kikitan</span>
                        <span className="text-md font-semibold text-muted -ml-1 hidden sm:inline">Translator</span>
                        <button type="button"
                                className="ml-1 text-2xs text-muted hover:text-accent-text transition-colors"
                                onClick={() => setChangelogsVisible(true)}>
                            v{appVersion}
                        </button>
                    </div>

                    <div className="flex-1"/>

                    <div role="tablist" className="flex p-1 gap-1 rounded-control bg-raised border border-line">
                        {[
                            {value: false, label: localization.translation[lang]},
                            {value: true, label: localization.stt_only[lang]},
                        ].map(opt => {
                            const active = appState.config.speech_to_text_only == opt.value
                            return <button key={String(opt.value)} type="button" role="tab" aria-selected={active}
                                           onClick={() => setConfig("speech_to_text_only", opt.value)}
                                           className={`h-7 px-3 rounded-[4px] text-2xs font-semibold transition-colors ${active ? "bg-accent text-accent-ink" : "text-muted hover:text-ink"}`}>
                                {opt.label}
                            </button>
                        })}
                    </div>

                    <div className="flex items-center gap-1">
                        <Tooltip title={localization.change_theme[lang]}>
                            <IconButton onClick={() => setConfig("light_mode", !appState.config.light_mode)}>
                                {appState.config.light_mode ? <NightsStay fontSize="small"/> : <WbSunny fontSize="small"/>}
                            </IconButton>
                        </Tooltip>
                        <Tooltip title={localization.mode_selection[lang]}>
                            <IconButton onClick={() => {
                                controlKikitan(false)
                                setConfig("quickstart_viewed", false)
                            }}>
                                <Translate fontSize="small"/>
                            </IconButton>
                        </Tooltip>
                        <Tooltip title={localization.settings[lang]}>
                            <IconButton onClick={() => {
                                controlKikitan(false)
                                setSettingsVisible(true)
                            }}>
                                <Settings fontSize="small"/>
                            </IconButton>
                        </Tooltip>
                    </div>
                </header>

                <main className="flex-1 min-h-0 flex flex-col">
                    {loaded && appState.config.quickstart_viewed && <Kikitan state={appState}/>}
                </main>
            </div>
        </ThemeProvider>
    )
}

export default App
