import { Config } from "tailwindcss";

export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      // Colours resolve to the CSS variables in globals.css, so light/dark is one data-theme switch
      colors: {
        bg: "var(--bg)",
        surface: "var(--surface)",
        raised: "var(--raised)",
        line: "var(--line)",
        "line-strong": "var(--line-strong)",
        ink: "var(--ink)",
        muted: "var(--muted)",
        faint: "var(--faint)",
        accent: "var(--accent)",
        "accent-hover": "var(--accent-hover)",
        "accent-text": "var(--accent-text)",
        "accent-ink": "var(--accent-ink)",
        "accent-soft": "var(--accent-soft)",
        success: "var(--success)",
        danger: "var(--danger)",
        warning: "var(--warning)",
        scrim: "var(--scrim)",
      },
      borderRadius: {
        control: "6px",
        panel: "14px",
      },
      boxShadow: {
        panel: "var(--shadow)",
      },
      fontSize: {
        // 1.2 ratio scale for dense product UI
        "2xs": ["12px", "16px"],
        sm: ["14px", "20px"],
        md: ["17px", "26px"],
        lg: ["20px", "30px"],
        xl: ["24px", "34px"],
        "2xl": ["29px", "38px"],
      },
    },
  },
  plugins: [],
} satisfies Config;
