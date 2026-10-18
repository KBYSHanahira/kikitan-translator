import { createTheme } from "@mui/material/styles";

// MUI theme mirroring the CSS tokens in globals.css.
// Component colours use var(--token) so MUI and Tailwind always agree;
// palette hex values are only there because MUI needs real colours to derive states.
export function buildTheme(light: boolean) {
    const accent = light ? "#F06A0C" : "#FF7A1A";

    return createTheme({
        palette: {
            mode: light ? "light" : "dark",
            primary: { main: accent, contrastText: "#1A0D04" },
            secondary: { main: light ? "#1A120C" : "#F4EDE6" },
            success: { main: light ? "#14995B" : "#3DD68C" },
            error: { main: light ? "#D93636" : "#FF5C5C" },
            warning: { main: light ? "#B87100" : "#FFB547" },
            background: {
                default: light ? "#F7F3EF" : "#0D0A08",
                paper: light ? "#FFFFFF" : "#16110E",
            },
            text: {
                primary: light ? "#1A120C" : "#F4EDE6",
                secondary: light ? "#66574B" : "#A3968B",
            },
            divider: light ? "#E6DCD2" : "#2D231C",
        },
        shape: { borderRadius: 6 },
        typography: {
            fontFamily: `"Inter", "Yu Gothic UI", "Microsoft YaHei UI", "Malgun Gothic", system-ui, sans-serif`,
            fontSize: 14,
            button: { textTransform: "none", fontWeight: 600, letterSpacing: 0 },
        },
        components: {
            MuiButton: {
                defaultProps: { disableElevation: true },
                styleOverrides: {
                    root: { borderRadius: 6, minHeight: 36, paddingInline: 14 },
                    outlined: {
                        borderColor: "var(--line-strong)",
                        color: "var(--ink)",
                        "&:hover": { borderColor: "var(--accent)", backgroundColor: "var(--accent-soft)" },
                    },
                    containedPrimary: {
                        "&:hover": { backgroundColor: "var(--accent-hover)" },
                    },
                },
            },
            MuiIconButton: {
                styleOverrides: {
                    root: {
                        borderRadius: 6,
                        color: "var(--muted)",
                        "&:hover": { color: "var(--ink)", backgroundColor: "var(--raised)" },
                    },
                },
            },
            MuiOutlinedInput: {
                styleOverrides: {
                    root: {
                        borderRadius: 6,
                        backgroundColor: "var(--surface)",
                        "& .MuiOutlinedInput-notchedOutline": { borderColor: "var(--line-strong)" },
                        "&:hover .MuiOutlinedInput-notchedOutline": { borderColor: "var(--faint)" },
                        "&.Mui-focused .MuiOutlinedInput-notchedOutline": { borderColor: "var(--accent)", borderWidth: 2 },
                        "&.Mui-disabled": { opacity: 0.5 },
                    },
                    input: { paddingTop: 9, paddingBottom: 9 },
                },
            },
            MuiSelect: {
                styleOverrides: {
                    icon: { color: "var(--muted)" },
                },
            },
            MuiMenu: {
                styleOverrides: {
                    paper: {
                        backgroundColor: "var(--raised)",
                        backgroundImage: "none",
                        border: "1px solid var(--line)",
                        boxShadow: "var(--shadow)",
                        maxHeight: 360,
                    },
                },
            },
            MuiMenuItem: {
                styleOverrides: {
                    root: {
                        fontSize: 14,
                        "&.Mui-selected": { backgroundColor: "var(--accent-soft)", color: "var(--accent-text)" },
                        "&.Mui-selected:hover": { backgroundColor: "var(--accent-soft)" },
                    },
                },
            },
            MuiTooltip: {
                styleOverrides: {
                    tooltip: {
                        backgroundColor: "var(--ink)",
                        color: "var(--bg)",
                        fontSize: 12,
                        borderRadius: 6,
                    },
                },
            },
            MuiCheckbox: {
                styleOverrides: {
                    root: { color: "var(--faint)" },
                },
            },
            MuiSwitch: {
                styleOverrides: {
                    track: { backgroundColor: "var(--line-strong)", opacity: 1 },
                },
            },
            MuiTabs: {
                styleOverrides: {
                    indicator: { backgroundColor: "var(--accent)", height: 2 },
                },
            },
            MuiTab: {
                styleOverrides: {
                    root: {
                        textTransform: "none",
                        fontWeight: 600,
                        fontSize: 14,
                        color: "var(--muted)",
                        minHeight: 48,
                        "&.Mui-selected": { color: "var(--ink)" },
                    },
                },
            },
            MuiAlert: {
                styleOverrides: {
                    root: { borderRadius: 6 },
                },
            },
            MuiCircularProgress: {
                defaultProps: { color: "inherit" },
            },
        },
    });
}
