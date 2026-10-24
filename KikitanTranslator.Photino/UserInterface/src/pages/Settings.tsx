import * as React from "react"

import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';

import {
    IconButton,
    Switch,
    TextField,
    Select,
    MenuItem,
    Button,
    Tooltip,
    Slider
} from "@mui/material";

import {
    History,
    OpenInNew,
    FolderOpen,
} from '@mui/icons-material';

import {localization} from "../util/localization";
import {openURL, pickRvcModel, setConfig} from "../util/photino.ts";
import {app_state} from "../util/constants.ts";
import {ModalHeader} from "../components/Modal.tsx";

type CustomTabPanelProps = {
    children: React.ReactNode;
    value: number;
    index: number;
};

function CustomTabPanel({children, value, index}: CustomTabPanelProps) {
    return (
        <div
            role="tabpanel"
            hidden={value !== index}
            id={`simple-tabpanel-${index}`}
            aria-labelledby={`simple-tab-${index}`}
            className="flex-1 min-h-0 overflow-y-auto"
        >
            {value === index && <div className="p-6 flex flex-col gap-6 max-w-[760px]">{children}</div>}
        </div>
    );
}

function a11yProps(index: number) {
    return {
        id: `simple-tab-${index}`,
        'aria-controls': `simple-tabpanel-${index}`,
    };
}

// One boolean setting: label + explanation on the left, switch on the right
function ToggleRow({label, hint, checked, onChange}: {
    label: string,
    hint?: string,
    checked: boolean,
    onChange: (v: boolean) => void
}) {
    return (
        <label className="flex items-start gap-4 py-3 border-b border-line last:border-b-0 cursor-pointer">
            <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold">{label}</p>
                {hint && <p className="text-2xs text-muted mt-0.5">{hint}</p>}
            </div>
            <Switch checked={checked} onChange={(e) => onChange(e.target.checked)}/>
        </label>
    );
}

function Field({label, children}: { label: string, children: React.ReactNode }) {
    return (
        <div className="flex flex-col gap-2">
            <p className="text-2xs font-semibold text-muted">{label}</p>
            {children}
        </div>
    );
}

function Group({children}: { children: React.ReactNode }) {
    return <div className="rounded-panel border border-line bg-bg px-4">{children}</div>;
}

type SettingsProps = {
    closeCallback: () => void;
    state: app_state;
}

export default function SettingsPage({ closeCallback, state }: SettingsProps) {
    const [page, setPage] = React.useState(0);
    const lang = state.config.language;
    // Voices for the language that is spoken: the translation, or the original text in speech-to-text-only mode
    const spokenLanguage = state.config.speech_to_text_only ? state.config.source_language : state.config.target_language;
    const ttsVoices = state.voices.filter(v => v.language == spokenLanguage);

    // The slider moves locally while dragging and saves once on release
    const [transpose, setTranspose] = React.useState(state.config.rvc_transpose);
    React.useEffect(() => setTranspose(state.config.rvc_transpose), [state.config.rvc_transpose]);

    const rvc = state.rvc_status;
    const rvcStatusText = rvc.state == "downloading" ? `${localization.rvc_downloading[lang]} ${rvc.detail} … ${Math.floor(rvc.progress * 100)}%`
        : rvc.state == "installing" ? `${localization.rvc_installing[lang]} ${rvc.detail}${rvc.progress > 0 && rvc.progress < 1 ? ` … ${Math.floor(rvc.progress * 100)}%` : " …"}`
        : rvc.state == "converting" ? `${localization.rvc_converting[lang]} ${rvc.detail} …`
        : rvc.state == "loading" ? localization.rvc_loading[lang]
        : rvc.state == "ready" ? `${localization.rvc_ready[lang]} · ${rvc.detail}`
        : rvc.state == "error" ? `${localization.rvc_error[lang]}: ${rvc.detail}`
        : "";

    const handleChange = (_event: React.SyntheticEvent, newValue: number) => {
        setPage(newValue);
    };

    const apiKeyField = (name: string, value: string, field: string, enabled: boolean, keyUrl: string) => (
        <Field label={`${name} ${localization.api_key[lang]}`}>
            <div className="flex gap-2">
                <TextField
                    size="small"
                    fullWidth
                    value={value}
                    disabled={!enabled}
                    type="password"
                    color={value.length == 0 ? "warning" : "primary"}
                    onChange={(e) => setConfig(field, e.target.value)}
                />
                <Button variant={value.length == 0 && enabled ? "contained" : "outlined"}
                        disabled={!enabled}
                        endIcon={<OpenInNew fontSize="small"/>}
                        sx={{ whiteSpace: "nowrap", flexShrink: 0 }}
                        onClick={() => openURL(keyUrl)}>
                    {localization.get_api_key[lang]}
                </Button>
            </div>
        </Field>
    );

    return <>
        <ModalHeader title={localization.settings[lang]} closeLabel={localization.close[lang]} onClose={closeCallback}/>
        <div className="px-3 border-b border-line shrink-0">
            <Tabs value={page} onChange={handleChange} variant="scrollable" scrollButtons="auto">
                <Tab label={localization.vrchat_settings[lang]} {...a11yProps(0)} />
                <Tab label={localization.translator_settings[lang]} {...a11yProps(1)} />
                <Tab label={localization.text_to_speech[lang]} {...a11yProps(2)} />
                <Tab label={localization.advanced_settings[lang]} {...a11yProps(3)} />
            </Tabs>
        </div>

        <CustomTabPanel value={page} index={0}>
            <Group>
                <ToggleRow label={localization.only_send_translation[lang]}
                           hint={localization.do_not_send_original_tooltip[lang]}
                           checked={state.config.translation_only}
                           onChange={(v) => setConfig("translation_only", v)}/>
                <ToggleRow label={localization.disable_kikitan_when_muted[lang]}
                           hint={localization.disable_kikitan_when_muted_tooltip[lang]}
                           checked={state.config.disable_when_muted}
                           onChange={(v) => setConfig("disable_when_muted", v)}/>
                <ToggleRow label={localization.send_to_chatbox_without_waiting[lang]}
                           hint={localization.send_to_chatbox_without_waiting_tooltip[lang]}
                           checked={state.config.send_without_waiting_for_finish}
                           onChange={(v) => setConfig("send_without_waiting_for_finish", v)}/>
            </Group>
            <Field label={localization.chatbox_update_speed[lang]}>
                <Select size="small" sx={{width: 200}} value={state.config.chatbox_wait_per_char_ms}
                        onChange={(e) => setConfig("chatbox_wait_per_char_ms", e.target.value)}>
                    <MenuItem value={60}>{localization.slow[lang]}</MenuItem>
                    <MenuItem value={30}>{localization.medium[lang]}</MenuItem>
                    <MenuItem value={15}>{localization.fast[lang]}</MenuItem>
                </Select>
            </Field>
        </CustomTabPanel>

        <CustomTabPanel value={page} index={1}>
            <div className="grid grid-cols-2 gap-x-8 gap-y-6">
                <Field label={localization.translation_service[lang]}>
                    <Select size="small" fullWidth value={state.config.translator}
                            onChange={(e) => setConfig("translator", e.target.value)}>
                        <MenuItem value={0}>Google Translate ({localization.default[lang]})</MenuItem>
                        <MenuItem value={1}>Groq ({localization.requires_free_api_key[lang]})</MenuItem>
                        <MenuItem value={2}>Gemini ({localization.requires_free_api_key[lang]})</MenuItem>
                    </Select>
                </Field>
                <Field label={localization.recognition_service[lang]}>
                    <Select size="small" fullWidth value={state.config.recognizer}
                            onChange={(e) => setConfig("recognizer", e.target.value)}>
                        <MenuItem value={0}>Microsoft Bing ({localization.default[lang]})</MenuItem>
                        <MenuItem value={1}>Groq ({localization.requires_free_api_key[lang]})</MenuItem>
                        <MenuItem value={2}>Gemini ({localization.requires_free_api_key[lang]})</MenuItem>
                    </Select>
                </Field>
                {apiKeyField("Groq", state.config.groq_api_key, "groq_api_key",
                    state.config.translator == 1 || state.config.recognizer == 1, "https://console.groq.com/keys")}
                {apiKeyField("Gemini", state.config.gemini_api_key, "gemini_api_key",
                    state.config.translator == 2 || state.config.recognizer == 2, "https://aistudio.google.com/api-keys")}
            </div>
        </CustomTabPanel>

        <CustomTabPanel value={page} index={2}>
            <Group>
                <ToggleRow label={localization.text_to_speech[lang]}
                           hint={localization.text_to_speech_tooltip[lang]}
                           checked={state.config.tts_enabled}
                           onChange={(v) => setConfig("tts_enabled", v)}/>
            </Group>
            <Field label={localization.tts_voice[lang]}>
                <Select size="small" sx={{width: 320}} displayEmpty
                        value={ttsVoices.some(v => v.id == state.config.tts_voice) || state.config.tts_voice == "google" ? state.config.tts_voice : ""}
                        onChange={(e) => setConfig("tts_voice", e.target.value)}>
                    <MenuItem value="">{localization.tts_voice_auto[lang]}</MenuItem>
                    {ttsVoices.map((voice) => (
                        <MenuItem key={voice.id} value={voice.id}>{voice.name}</MenuItem>
                    ))}
                    <MenuItem value="google">{localization.tts_voice_google[lang]}</MenuItem>
                </Select>
                {ttsVoices.length == 0 &&
                    <p className="text-2xs text-warning">{localization.tts_no_windows_voice[lang]}</p>}
                <p className="text-2xs text-muted">{localization.tts_voice_hint[lang]}</p>
            </Field>

            <Group>
                <ToggleRow label={localization.rvc_enabled[lang]}
                           hint={localization.rvc_hint[lang]}
                           checked={state.config.rvc_enabled}
                           onChange={(v) => setConfig("rvc_enabled", v)}/>
            </Group>
            <Field label={localization.rvc_model[lang]}>
                <div className="flex gap-2">
                    <TextField size="small" fullWidth disabled={!state.config.rvc_enabled}
                               value={state.config.rvc_model_path.split(/[\\/]/).pop() || localization.rvc_no_model[lang]}
                               title={state.config.rvc_model_path}
                               slotProps={{ input: { readOnly: true } }}/>
                    <Button variant={state.config.rvc_model_path.length == 0 && state.config.rvc_enabled ? "contained" : "outlined"}
                            disabled={!state.config.rvc_enabled}
                            startIcon={<FolderOpen fontSize="small"/>}
                            sx={{ whiteSpace: "nowrap", flexShrink: 0 }}
                            onClick={() => pickRvcModel()}>
                        {localization.browse[lang]}
                    </Button>
                </div>
                {state.config.rvc_enabled && rvcStatusText &&
                    <p className={`text-2xs ${state.rvc_status.state == "error" ? "text-warning" : "text-muted"}`}>{rvcStatusText}</p>}
            </Field>
            <Group>
                <ToggleRow label={localization.rvc_use_gpu[lang]}
                           hint={localization.rvc_use_gpu_hint[lang]}
                           checked={state.config.rvc_use_gpu}
                           onChange={(v) => setConfig("rvc_use_gpu", v)}/>
            </Group>
            <Field label={localization.rvc_transpose[lang]}>
                <div className="px-2 max-w-[420px]">
                    <Slider size="small" min={-24} max={24} step={1} disabled={!state.config.rvc_enabled}
                            value={transpose} valueLabelDisplay="auto"
                            marks={[{value: -12, label: "-12"}, {value: 0, label: "0"}, {value: 12, label: "+12"}]}
                            onChange={(_e, v) => setTranspose(v as number)}
                            onChangeCommitted={(_e, v) => setConfig("rvc_transpose", v as number)}/>
                </div>
                <p className="text-2xs text-muted">{localization.rvc_transpose_hint[lang]}</p>
            </Field>
        </CustomTabPanel>

        <CustomTabPanel value={page} index={3}>
            <Field label={localization.osc_port[lang]}>
                <div className="flex items-center gap-2">
                    <TextField size="small" sx={{width: 160}} value={state.config.osc_port} type="number"
                               onChange={(e) => setConfig("osc_port", parseInt(e.target.value))}/>
                    {state.config.osc_port != 9000 &&
                        <Tooltip title="9000">
                            <IconButton aria-label="Reset to 9000" onClick={() => setConfig("osc_port", 9000)}>
                                <History fontSize="small"/>
                            </IconButton>
                        </Tooltip>
                    }
                </div>
            </Field>
            <Group>
                <ToggleRow label={localization.enable_user_data[lang]}
                           checked={state.config.send_user_data}
                           onChange={(v) => setConfig("send_user_data", v)}/>
            </Group>
            <div>
                <Button variant="outlined" startIcon={<FolderOpen fontSize="small"/>}
                        onClick={() => openURL("LOGFILES")}>{localization.open_logs[lang]}</Button>
            </div>
        </CustomTabPanel>
    </>
}
