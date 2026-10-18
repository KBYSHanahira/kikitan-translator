import * as React from "react";

import {
    Select,
    MenuItem,
    Button,
    TextField,
    IconButton,
    Tooltip,
    CircularProgress,
    Snackbar,
    Alert,
    Slide,
    Switch,
} from "@mui/material";

import {
    X as XIcon,
    GitHub as GitHubIcon,
    Favorite as FavoriteIcon,
    KeyboardVoice as KeyboardVoiceIcon,
    PlayArrow as PlayArrowIcon,
    Pause as PauseIcon,
    Keyboard,
    History as HistoryIcon,
    Close as CloseIcon,
    Mic as MicIcon,
    Translate as TranslateIcon,
    SwapHoriz as SwapHorizIcon,
    Send as SendIcon,
} from "@mui/icons-material";

import {
    app_state,
    langSource,
    langTo,
} from "../util/constants";

import { localization } from "../util/localization";
import {
    controlKikitan,
    manualTranslate, openURL, registerMicrophoneChangedCallback, registerNotificationCallback,
    registerRecognitionCallback,
    setConfig
} from "../util/photino.ts";
import Modal, { ModalHeader } from "../components/Modal.tsx";

// Borderless select used inside panel headers
const headerSelectSx = {
    minWidth: 160,
    fontWeight: 600,
    "& .MuiOutlinedInput-notchedOutline": { border: "none" },
    "&:hover": { backgroundColor: "var(--raised)" },
    "&.Mui-focused .MuiOutlinedInput-notchedOutline": { border: "2px solid var(--accent)" },
    "& .MuiSelect-select": { paddingLeft: "8px", paddingTop: "6px", paddingBottom: "6px" },
};

export default function Kikitan({ state }: { state: app_state }) {
    const [messageHistory, setMessageHistory] = React.useState<{ source: string, translation: string, timestamp: number }[]>([])

    const [detecting, setDetecting] = React.useState(false);

    const [detection, setDetection] = React.useState<string>("");
    const [translation, setTranslation] = React.useState("");

    const [showMessageHistory, setShowMessageHistory] = React.useState(false);

    const [textInputVisible, setTextInputVisible] = React.useState(false);
    const [textInputValue, setTextInputValue] = React.useState("");

    const [notification, setNotification] = React.useState<{
        open: boolean;
        message: string;
        severity: "success" | "error" | "warning" | "info";
    }>({ open: false, message: "", severity: "info" });

    const lang = state.config.language;
    const running = state.status == 2;
    const starting = state.status == 1;
    const mutedPause = state.is_muted && state.config.disable_when_muted;

    const showNotification = (message: string, severity: "success" | "error" | "warning" | "info" = "info") => {
        switch (message) {
            case "GROQ_NO_API_KEY":
                setNotification({ open: true, message: localization.no_api_key_configured_for_groq[lang], severity });
                break;
            case "GROQ_INVALID_API_KEY":
                setNotification({ open: true, message: localization.invalid_api_key_groq[lang], severity });
                break;
            case "GEMINI_NO_API_KEY":
                setNotification({ open: true, message: localization.no_api_key_configured_for_gemini[lang], severity });
                break;
            case "GEMINI_INVALID_API_KEY":
                setNotification({ open: true, message: localization.invalid_api_key_gemini[lang], severity });
                break;
            default:
                setNotification({ open: true, message, severity });

                break;
        }
    };

    React.useEffect(() => {
        // @ts-ignore
        registerRecognitionCallback((r, t, f) => {
            if (r.length != 0) setDetection(r);
            if (t.length != 0) setTranslation(t);

            setDetecting(!f);

            if (f) {
                var copy = messageHistory;

                if (copy.length >= 50) copy = copy.slice(1)

                copy.unshift({
                    source: r,
                    translation: t,
                    timestamp: Date.now()
                })

                sessionStorage.setItem("history", JSON.stringify(copy))
                setMessageHistory(copy)
            }
        });

        registerMicrophoneChangedCallback(() => showNotification(localization.microphone_updated[lang], "warning"))
        registerNotificationCallback((msg: string, level: number) => showNotification(msg, level == 1 ? "warning" : level == 2 ? "error" : level == 3 ? "success" : "info"))
        controlKikitan(true)

        if (sessionStorage.getItem("history") != null) setMessageHistory(JSON.parse(sessionStorage.getItem("history")!))
    }, []);

    const formatTimestamp = (timestamp: number) => {
        const date = new Date(timestamp);
        return date.toLocaleTimeString();
    };

    const submitText = () => {
        manualTranslate(textInputValue)

        setTextInputVisible(false);
        setTextInputValue("");
    };

    const translationOff = state.config.speech_to_text_only;

    return (
        <>
            <Modal open={showMessageHistory} className="w-[min(720px,92vw)] h-[min(520px,85vh)]">
                <ModalHeader title={localization.message_history[lang]} closeLabel={localization.close[lang]}
                             onClose={() => setShowMessageHistory(false)}/>
                <div className="flex-1 min-h-0 overflow-y-auto p-5 select-text">
                    {messageHistory.length === 0 ? (
                        <div className="h-full grid place-items-center text-sm text-muted">
                            {localization.no_history[lang]}
                        </div>
                    ) : (
                        <ol className="list-none p-0 m-0 flex flex-col gap-3">
                            {messageHistory.map((item, index) => (
                                <li key={index} className="grid grid-cols-[72px_1fr] gap-4 p-4 rounded-control bg-raised border border-line">
                                    <span className="text-2xs text-muted pt-0.5 tabular-nums">{formatTimestamp(item.timestamp)}</span>
                                    <div className="min-w-0">
                                        <p className="text-sm text-muted break-words">{item.source}</p>
                                        {item.translation && <p className="text-md font-semibold mt-1 break-words">{item.translation}</p>}
                                    </div>
                                </li>
                            ))}
                        </ol>
                    )}
                </div>
            </Modal>

            <div className="flex-1 min-h-0 flex flex-col gap-4 p-5">
                {/* Stage: source and translation side by side */}
                <div className="flex-1 min-h-0 grid grid-cols-[1fr_auto_1fr] gap-3">
                    <section className={`min-h-0 flex flex-col rounded-panel border bg-surface transition-colors ${detecting ? "border-line-strong" : "border-line"}`}>
                        <div className="h-12 shrink-0 flex items-center gap-2 px-3 border-b border-line">
                            <MicIcon fontSize="small" className="text-muted" />
                            <Select size="small" sx={headerSelectSx} value={state.config.source_language}
                                    onChange={(e) => setConfig("source_language", e.target.value)}>
                                {langSource.map((element) => (
                                    <MenuItem key={element.code} value={element.code}>{element.name[lang]}</MenuItem>
                                ))}
                            </Select>
                            <div className="flex-1"/>
                            {running && detecting && <span className="live-dot" aria-hidden="true"/>}
                        </div>
                        <div className={`flex-1 min-h-0 overflow-y-auto p-5 select-text ${running ? "" : "opacity-50"}`}>
                            <p className={`text-lg font-semibold break-words ${detecting ? "text-muted" : "text-ink"}`}>
                                {detection}
                            </p>
                        </div>
                    </section>

                    <div className="flex items-center">
                        <IconButton aria-label="Swap languages" disabled={translationOff}
                                    sx={{ border: "1px solid var(--line)", backgroundColor: "var(--surface)" }}
                                    onClick={() => {
                                        setConfig("source_language", state.config.target_language)
                                        setConfig("target_language", state.config.source_language)
                                    }}>
                            <SwapHorizIcon fontSize="small"/>
                        </IconButton>
                    </div>

                    {/* Signature: orange top edge marks the output panel — the product's result */}
                    <section className={`relative min-h-0 flex flex-col rounded-panel border border-line bg-surface overflow-hidden ${translationOff ? "opacity-60" : ""}`}>
                        <span className={`absolute inset-x-0 top-0 h-[3px] ${translationOff ? "bg-line-strong" : "bg-accent"}`} aria-hidden="true"/>
                        <div className="h-12 shrink-0 flex items-center gap-2 px-3 border-b border-line">
                            <TranslateIcon fontSize="small" className={translationOff ? "text-muted" : "text-accent-text"} />
                            <Select size="small" sx={headerSelectSx} value={state.config.target_language}
                                    disabled={translationOff}
                                    onChange={(e) => setConfig("target_language", e.target.value)}>
                                {langTo.map((element) => (
                                    <MenuItem key={element.code} value={element.code}>{element.name[lang]}</MenuItem>
                                ))}
                            </Select>
                        </div>
                        <div className={`flex-1 min-h-0 overflow-y-auto p-5 select-text ${running ? "" : "opacity-50"}`}>
                            {translationOff
                                ? <p className="text-sm text-muted">{localization.translation_disabled[lang]}</p>
                                : <p className="text-xl font-extrabold tracking-tight break-words">{translation}</p>}
                        </div>
                    </section>
                </div>

                {/* Controls */}
                <div className="shrink-0 flex items-center justify-center gap-2 min-h-[44px]">
                    {textInputVisible ? (
                        <div className="w-full max-w-[640px] flex items-center gap-2">
                            <TextField
                                autoFocus
                                fullWidth
                                size="small"
                                placeholder={localization.type_here[lang]}
                                value={textInputValue}
                                onKeyDown={(e) => {
                                    if (e.key == "Enter") submitText();
                                    if (e.key == "Escape") { setTextInputVisible(false); setTextInputValue(""); }
                                }}
                                onChange={(e) => setTextInputValue(e.target.value)}
                            />
                            <Button variant="contained" startIcon={<SendIcon fontSize="small"/>} onClick={submitText}>
                                {localization.send[lang]}
                            </Button>
                            <IconButton aria-label={localization.close[lang]} onClick={() => {
                                setTextInputVisible(false);
                                setTextInputValue("");
                            }}>
                                <CloseIcon fontSize="small"/>
                            </IconButton>
                        </div>
                    ) : (
                        <>
                            <Tooltip title={localization.text[lang]}>
                                <span>
                                    <Button variant="outlined" disabled={!running} aria-label={localization.text[lang]}
                                            sx={{ minWidth: 44, height: 44 }}
                                            onClick={() => setTextInputVisible(true)}>
                                        <Keyboard fontSize="small"/>
                                    </Button>
                                </span>
                            </Tooltip>
                            <Tooltip title={mutedPause ? localization.translation_disabled_muted[lang] : ""}>
                                <span>
                                    <Button variant={running ? "outlined" : "contained"}
                                            color={state.is_muted ? "warning" : "primary"}
                                            disabled={starting}
                                            sx={{ minWidth: 168, height: 44, fontSize: 14 }}
                                            startIcon={starting ? <CircularProgress size={16}/> : running ? <PauseIcon/> : <PlayArrowIcon/>}
                                            onClick={() => controlKikitan(state.status == 0)}>
                                        {state.status == 0 ? localization.start[lang] : running ? localization.stop[lang] : "…"}
                                    </Button>
                                </span>
                            </Tooltip>
                            <Tooltip title={localization.message_history[lang]}>
                                <span>
                                    <Button variant="outlined" disabled={messageHistory.length == 0}
                                            aria-label={localization.message_history[lang]}
                                            sx={{ minWidth: 44, height: 44 }}
                                            onClick={() => setShowMessageHistory(true)}>
                                        <HistoryIcon fontSize="small"/>
                                    </Button>
                                </span>
                            </Tooltip>
                        </>
                    )}
                </div>
            </div>

            {/* Device bar */}
            <footer className="h-14 shrink-0 flex items-center gap-4 px-5 border-t border-line bg-surface">
                <div className="flex items-center gap-2 min-w-0">
                    <KeyboardVoiceIcon fontSize="small" className="text-muted shrink-0"/>
                    <Select size="small" sx={{ width: 220 }} value={state.config.microphone}
                            onChange={(e) => setConfig("microphone", e.target.value)}>
                        {state.microphones.map((element) => (
                            <MenuItem key={element.name} value={element.name}>
                                {(element.name.includes("(") && element.name.includes(")")) ? element.name.match(/\(([^)]+)\)/)?.[1] : element.name}{element.default ? ` (${localization.default[lang]})` : ""}
                            </MenuItem>
                        ))}
                    </Select>
                </div>

                <Tooltip title={state.is_linux ? localization.desktop_translation_warning[lang] : ""}>
                    <label className={`flex items-center gap-1 text-sm ${state.config.desktop_translation ? "text-ink" : "text-muted"}`}>
                        <Switch size="small" checked={state.config.desktop_translation} disabled={state.is_linux}
                                onChange={e => setConfig("desktop_translation", e.target.checked)}/>
                        {state.config.desktop_translation ? localization.desktop_capture_enabled[lang] : localization.enable_desktop_capture[lang]}
                    </label>
                </Tooltip>

                <div className="flex-1"/>

                <div className="flex items-center gap-0.5">
                    <IconButton size="small" aria-label="X" onClick={() => openURL("https://twitter.com/marquina_osu")}>
                        <XIcon fontSize="small"/>
                    </IconButton>
                    <IconButton size="small" aria-label="Buy Me a Coffee" onClick={() => openURL("https://buymeacoffee.com/sergiomarquina")}>
                        <FavoriteIcon fontSize="small"/>
                    </IconButton>
                    <IconButton size="small" aria-label="GitHub" onClick={() => openURL("https://github.com/YusufOzmen01/kikitan-translator")}>
                        <GitHubIcon fontSize="small"/>
                    </IconButton>
                    <IconButton size="small" aria-label="Discord" onClick={() => openURL("https://discord.gg/jpkYCgpBGV")}>
                        <img src="/discordlogo.webp" width={18} height={18} alt=""
                             className={state.config.light_mode ? "opacity-70" : "invert opacity-70"}/>
                    </IconButton>
                </div>
            </footer>

            <Snackbar
                open={notification.open}
                autoHideDuration={5000}
                onClose={() => setNotification(prev => ({ ...prev, open: false }))}
                anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
                TransitionComponent={Slide}
                sx={{ bottom: { xs: 72 } }}
            >
                <Alert
                    onClose={() => setNotification(prev => ({ ...prev, open: false }))}
                    severity={notification.severity}
                    variant="filled"
                    sx={{ width: "100%" }}
                >
                    {notification.message}
                </Alert>
            </Snackbar>
        </>
    );
}
