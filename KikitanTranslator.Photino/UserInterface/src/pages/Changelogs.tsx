// @ts-ignore
import * as React from "react"

import { CircularProgress } from "@mui/material";

import { localization } from "../util/localization";

import {app_state} from "../util/constants";
import { fetchURL } from "../util/photino";
import {ModalHeader} from "../components/Modal";

type ChangelogsProps = {
    closeCallback: () => void;
    state: app_state;
}

export default function Changelogs({ closeCallback, state }: ChangelogsProps) {
    const [changelog, setChangelog] = React.useState("")
    const [fetched, setFetched] = React.useState(false)

    React.useEffect(() => {
        // Fetch handler resolves "" on failure, so loading is tracked separately from content
        fetchURL(`https://github.com/YusufOzmen01/kikitan-translator/releases/download/v${state.app_version}/CHANGELOG_${state.config.language}.html`).then(c => {
            setChangelog(c)
            setFetched(true)
        })
    }, [])

    return <>
        <ModalHeader title={localization.changelogs[state.config.language]}
                     closeLabel={localization.close[state.config.language]}
                     onClose={closeCallback}>
            <span className="text-2xs font-semibold px-2 py-0.5 rounded-full bg-accent-soft text-accent-text">v{state.app_version}</span>
        </ModalHeader>

        {!fetched
            ? <div className="flex-1 grid place-items-center text-muted"><CircularProgress size={24}/></div>
            : changelog.length == 0
            ? <div className="flex-1 grid place-items-center text-sm text-muted">—</div>
            : <div className="changelog-body flex-1 min-h-0 overflow-y-auto px-8 py-6 text-sm leading-relaxed whitespace-pre text-wrap select-text"
                   dangerouslySetInnerHTML={{__html: changelog}}></div>}
    </>
}
