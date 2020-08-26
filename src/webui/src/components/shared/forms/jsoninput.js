import React from "react";
import JSONInput from "react-json-editor-ajrm";
import locale from "react-json-editor-ajrm/locale/en";

const onChangeSelect = (onChange) => (value) => {
        onChange({ target: { value: value } });
    },
    themeSwitch = (theme) => {
        switch (theme) {
            // see https://github.com/AndrewRedican/react-json-editor-ajrm/wiki/Built-In-Themes
            // I hardcoded these colors from our scss themes. I'm sorry, I couldn't figure out a better way to do it.
            case "mmm":
            case "light":
                return {
                    colors: {
                        default: "#000",
                        background: "#fff",
                        background_warning: "#fff",
                        string: "#fa7921",
                        number: "#70ce35",
                        colon: "#49b8f7",
                        keys: "#59a5d8",
                        keys_whitespace: "#835fb6",
                        primitive: "#386fa4",
                        error: "#f00",
                    },
                };
            case "dark":
                return {
                    theme: "dark_vscode_tribute",
                };
            default:
                return undefined;
        }
    };

export const JsonInput = (props) => {
    debugger;
    const { id, onChange, value, height, width, theme, readOnly } = props,
        { colors, theme: inputTheme } = themeSwitch(theme);

    return (
        <JSONInput
            id={id}
            placeholder={value === "" ? undefined : value.jsObject}
            height={height || "200px"}
            width={width || "100%"}
            onChange={onChangeSelect(onChange)}
            locale={locale}
            colors={colors || {}}
            theme={inputTheme}
            viewOnly={readOnly}
        />
    );
};
