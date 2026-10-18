import React from "react"
import ReactDOM from "react-dom/client";
import App from "./page";

import "@fontsource/inter/400.css";
import "@fontsource/inter/600.css";
import "@fontsource/inter/800.css";
import "./globals.css";
import {init} from "./util/photino.ts";

// To get rid of TS compilation errors
(() => { return React.StrictMode })();

init();

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <App />
);
