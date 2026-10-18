/* eslint-disable react/react-in-jsx-scope */
"use client";

import "../style/carousel.css"

export default function Scroll() {
    return (
        <div className="carousel-container w-max">
            <div className="text-2xl font-extrabold tracking-tight text-center text-ink">
                {[
                    "Choose your language",
                    "言語を選択してください",
                    "选择语言",
                    "언어를 선택하세요",
                    "Choose your language",

                ].map((item, index) => (
                    <div key={index} className="carousel-item">
                        {item}
                    </div>
                ))}
            </div>
        </div>
    );
}
