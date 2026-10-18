import * as React from "react"

import {
    GitHub,
    Translate
} from '@mui/icons-material';

import Scroll from "./Scroll"
import { localization } from "../util/localization";
import { Button, MenuItem, Select } from "@mui/material";
import {openURL, setConfig} from "../util/photino.ts";
import {app_state} from "../util/constants.ts";

const LAST_PAGE = 6;

function Step({ active, children }: { active: boolean, children: React.ReactNode }) {
    return (
        <div className={`absolute inset-0 flex flex-col items-center justify-center gap-5 px-10 text-center transition-opacity duration-300 ${active ? "opacity-100" : "opacity-0 pointer-events-none"}`}>
            {children}
        </div>
    );
}

function StepTitle({ children }: { children: React.ReactNode }) {
    return <h2 className="text-xl font-extrabold tracking-tight max-w-[560px]">{children}</h2>;
}

function StepBody({ children }: { children: React.ReactNode }) {
    return <p className="text-sm text-muted max-w-[560px]">{children}</p>;
}

function Media({ children }: { children: React.ReactNode }) {
    return <div className="rounded-panel overflow-hidden border border-line bg-bg">{children}</div>;
}

export default function QuickstartMenu({ state }: { state: app_state }) {
    const [quickstartPage, setQuickstartPage] = React.useState(0)
    const lang = state.config.language

    return <>
        <div className="relative flex-1 min-h-0">
            <Step active={quickstartPage == 0}>
                <span className="w-12 h-12 rounded-panel bg-accent text-accent-ink grid place-items-center">
                    <Translate/>
                </span>
                {quickstartPage == 0 && !state.config.quickstart_viewed && <Scroll/>}
                <Select sx={{ minWidth: 220 }} value={lang} onChange={(e) => setConfig("language", e.target.value)}>
                    <MenuItem value={"en"}>English</MenuItem>
                    <MenuItem value={"jp"}>日本語</MenuItem>
                    <MenuItem value={"cn"}>中文</MenuItem>
                    <MenuItem value={"kr"}>한국어</MenuItem>
                </Select>
            </Step>

            <Step active={quickstartPage == 1}>
                <StepTitle>{localization.quickstart_osc[lang]}</StepTitle>
                {quickstartPage == 1 &&
                    <Media>
                        <video width={440} autoPlay loop muted className="block">
                            <source src="/OSC.mp4" type="video/mp4"></source>
                        </video>
                    </Media>
                }
            </Step>

            <Step active={quickstartPage == 2}>
                <StepTitle>{localization.mode_selection[lang]}</StepTitle>
                <Media>
                    <img className="block w-[420px] h-[280px]" alt="" src={
                        {
                            en: "/images/en.png",
                            jp: "/images/jp.png",
                            cn: "/images/cn.png",
                            kr: "/images/kr.png",
                        }[lang]
                    } />
                </Media>
                <StepBody>{localization.mode_selection_details[lang]}</StepBody>
            </Step>

            <Step active={quickstartPage == 3}>
                <StepTitle>{localization.desktop_translation[lang]}</StepTitle>
                <Media>
                    <img className="block w-[420px] h-[280px]" alt="" src={
                        {
                            en: "/images/en2.png",
                            jp: "/images/jp2.png",
                            cn: "/images/cn2.png",
                            kr: "/images/kr2.png",
                        }[lang]
                    } />
                </Media>
                <StepBody>{localization.desktop_translation_details[lang]}</StepBody>
            </Step>

            <Step active={quickstartPage == 4}>
                <StepTitle>{localization.settings[lang]}</StepTitle>
                <Media>
                    <img className="block w-[420px] h-[280px]" alt="" src="/images/settings.png" />
                </Media>
                <StepBody>{localization.settings_details[lang]}</StepBody>
            </Step>

            <Step active={quickstartPage == 5}>
                <StepTitle>{localization.change_theme[lang]}</StepTitle>
                <Media>
                    <img className="block w-[420px] h-[280px]" alt="" src="/images/theme.png" />
                </Media>
                <StepBody>{localization.change_theme_detail[lang]}</StepBody>
            </Step>

            <Step active={quickstartPage == LAST_PAGE}>
                <StepTitle>{localization.thank_you[lang]}</StepTitle>
                <StepBody>{localization.thank_you_details[lang]}</StepBody>
                <div className="flex flex-wrap justify-center gap-2 mt-2">
                    <Button disabled={quickstartPage != LAST_PAGE} variant="outlined" startIcon={<GitHub/>}
                            onClick={() => openURL("https://github.com/KBYSHanahira/kikitan-translator")}>
                        {localization.open_repo[lang]}
                    </Button>
                </div>
            </Step>
        </div>

        <div className="h-16 shrink-0 flex items-center gap-4 px-5 border-t border-line">
            <Button variant="text" color="inherit" disabled={quickstartPage == 0}
                    onClick={() => setQuickstartPage(quickstartPage - 1)}>
                {localization.previous[lang]}
            </Button>

            {/* Progress: one segment per step, filled up to the current one */}
            <div className="flex-1 flex justify-center gap-1.5" aria-label={`${quickstartPage + 1} / ${LAST_PAGE + 1}`}>
                {Array.from({ length: LAST_PAGE + 1 }, (_, i) => (
                    <span key={i}
                          className={`h-1 rounded-full transition-all duration-300 ${i == quickstartPage ? "w-6 bg-accent" : i < quickstartPage ? "w-3 bg-accent opacity-50" : "w-3 bg-line-strong"}`}/>
                ))}
            </div>

            <Button variant="contained" onClick={() => {
                if (quickstartPage >= LAST_PAGE) {
                    setConfig("quickstart_viewed", true)
                    setQuickstartPage(0)
                }
                else setQuickstartPage(quickstartPage + 1)
            }}>{quickstartPage >= LAST_PAGE ? localization.close[lang] : localization.next[lang]}</Button>
        </div>
    </>
}
