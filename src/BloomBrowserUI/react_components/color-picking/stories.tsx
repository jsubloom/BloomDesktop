// Don't add /** @jsxFrag React.Fragment */ or these stories won't show up in StoryBook! (at least in Aug 2022)
/** @jsx jsx **/
import { jsx, css } from "@emotion/core";

import * as React from "react";
import { storiesOf } from "@storybook/react";
import { useState } from "react";
import BloomButton from "../bloomButton";
import ColorPicker from "./colorPicker";
import { IColorInfo, getBackgroundColorCssFromColorInfo } from "./colorSwatch";
import {
    showColorPickerDialog,
    IColorPickerDialogProps,
    ColorDisplayButton,
    DialogResult
} from "./colorPickerDialog";
import { BloomPalette, TextBackgroundColors } from "./bloomPalette";
import BloomSketchPicker from "./bloomSketchPicker";
import tinycolor = require("tinycolor2");
import { ColorResult } from "react-color";
import { Typography } from "@material-ui/core";
import { HexColorInput } from "./hexColorInput";

const mainBlockStyles: React.CSSProperties = {
    width: 300,
    height: 300,
    display: "flex",
    flexDirection: "column",
    justifyContent: "center"
};

const backDivStyles: React.CSSProperties = {
    position: "relative",
    flex: 3,
    display: "flex",
    background: "lightgreen"
};

const chooserStyles: React.CSSProperties = {
    position: "absolute",
    top: 80,
    left: 400,
    width: 250,
    height: 310,
    border: "2px solid green",
    display: "flex",
    justifyContent: "center",
    alignContent: "center"
};

const initialOverDivStyles: React.CSSProperties = {
    position: "absolute",
    top: 10,
    left: 15,
    width: 120,
    height: 70,
    border: "1px solid red",
    zIndex: 2,
    background: "#fff"
};

storiesOf("Colors", module)
    .add("Background/text color", () =>
        React.createElement(() => {
            const [chooserShowing, setChooserShowing] = useState(false);
            const [backgroundChooser, setBackgroundChooser] = useState(true); // false is text chooser
            const [overDivStyles, setOverDivStyles] = useState(
                initialOverDivStyles
            );
            const [
                chooserCurrentBackgroundColor,
                setChooserCurrentBackgroundColor
            ] = useState<IColorInfo>({
                name: "white",
                colors: ["#ffffff"],
                opacity: 1
            });
            const [
                chooserCurrentTextColor,
                setChooserCurrentTextColor
            ] = useState<IColorInfo>({
                name: "black",
                colors: ["#000000"],
                opacity: 1
            });
            const handleColorChange = (
                color: IColorInfo,
                colorIsBackground: boolean
            ) => {
                if (colorIsBackground) {
                    // set background color
                    setOverDivStyles({
                        ...overDivStyles,
                        background: getBackgroundColorCssFromColorInfo(color)
                    });
                    setChooserCurrentBackgroundColor(color);
                } else {
                    const textColor = color.colors[0]; // don't need gradients or opacity for text color
                    // set text color
                    setOverDivStyles({
                        ...overDivStyles,
                        color: textColor
                    });
                    setChooserCurrentTextColor(color);
                }
            };

            return (
                <div style={mainBlockStyles}>
                    <div id="background-image" style={backDivStyles}>
                        I am a background "image" with lots of text so we can
                        test transparency.
                    </div>
                    <div id="set-my-background" style={overDivStyles}>
                        Set my text and background colors with the buttons
                    </div>
                    <div
                        style={{
                            flexDirection: "row",
                            display: "inline-flex",
                            justifyContent: "space-around"
                        }}
                    >
                        <BloomButton
                            onClick={() => {
                                setBackgroundChooser(true);
                                setChooserShowing(!chooserShowing);
                            }}
                            enabled={true}
                            hasText={true}
                            l10nKey={"dummyKey"}
                        >
                            Background
                        </BloomButton>
                        <BloomButton
                            onClick={() => {
                                setBackgroundChooser(false);
                                setChooserShowing(!chooserShowing);
                            }}
                            enabled={true}
                            hasText={true}
                            l10nKey={"dummyKey"}
                        >
                            Text
                        </BloomButton>
                    </div>
                    {chooserShowing && (
                        <div style={chooserStyles}>
                            <ColorPicker
                                onChange={color =>
                                    handleColorChange(color, backgroundChooser)
                                }
                                currentColor={
                                    backgroundChooser
                                        ? chooserCurrentBackgroundColor
                                        : chooserCurrentTextColor
                                }
                                swatchColors={TextBackgroundColors}
                                noAlphaSlider={!backgroundChooser}
                                noGradientSwatches={!backgroundChooser}
                            />
                        </div>
                    )}
                </div>
            );
        })
    )
    .add("Color Picker Dialog", () =>
        React.createElement(() => {
            const [overDivStyles, setOverDivStyles] = useState(
                initialOverDivStyles
            );
            const [
                chooserCurrentBackgroundColor,
                setChooserCurrentBackgroundColor
            ] = useState<IColorInfo>({
                name: "white",
                colors: ["#ffffff"],
                opacity: 1
            });
            const handleColorChange = (color: IColorInfo) => {
                console.log("Color change:");
                console.log(
                    `  ${color.name}: ${color.colors[0]}, ${color.colors[1]}, ${color.opacity}`
                );
                // set background color
                setOverDivStyles({
                    ...overDivStyles,
                    background: getBackgroundColorCssFromColorInfo(color)
                });
                setChooserCurrentBackgroundColor(color);
            };

            const colorPickerDialogProps: IColorPickerDialogProps = {
                localizedTitle: "Custom Color Picker",
                initialColor: chooserCurrentBackgroundColor,
                palette: BloomPalette.TextBackground,
                onChange: color => handleColorChange(color),
                onInputFocus: () => {}
            };

            return (
                <div style={mainBlockStyles}>
                    <div id="background-image" style={backDivStyles}>
                        I am a background "image" with lots of text so we can
                        test transparency.
                    </div>
                    <div id="set-my-background" style={overDivStyles}>
                        Set my text and background colors with the button
                    </div>
                    <div id="modal-container" />
                    <BloomButton
                        onClick={() =>
                            showColorPickerDialog(
                                colorPickerDialogProps,
                                document.getElementById("modal-container")
                            )
                        }
                        enabled={true}
                        hasText={true}
                        l10nKey={"dummyKey"}
                    >
                        Open Color Picker Dialog
                    </BloomButton>
                </div>
            );
        })
    )
    .add("Cover ColorDisplayButton", () =>
        React.createElement(() => {
            const initialColor = "#aa0000";
            const [currentColor, setCurrentColor] = useState(initialColor);

            return (
                <div
                    css={css`
                        background-color: lightyellow;
                        align-items: center;
                        border: 1px solid black;
                    `}
                    style={mainBlockStyles}
                >
                    <ColorDisplayButton
                        onClose={(result: DialogResult, newColor: string) => {
                            setCurrentColor(newColor);
                            alert(
                                `DialogResult was ${
                                    result === DialogResult.OK ? "OK" : "Cancel"
                                }`
                            );
                        }}
                        initialColor={currentColor}
                        noAlphaSlider={true}
                        localizedTitle="Test Color Button"
                        width={75}
                        palette={BloomPalette.CoverBackground}
                    />
                    <div
                        css={css`
                            background-color: ${currentColor};
                            width: 150px;
                            height: 32px;
                            padding: 5px;
                            margin-top: 20px;
                        `}
                    >
                        Chosen color block
                    </div>
                </div>
            );
        })
    )
    .add("BloomSketchPicker no transparency", () =>
        React.createElement(() => {
            const initialColor = "#aa0000";
            const [currentColor, setCurrentColor] = useState<IColorInfo>({
                colors: [initialColor],
                opacity: 1
            });

            const handlePickerChange = (color: ColorResult) => {
                const newColor: IColorInfo = {
                    colors: [color.hex],
                    opacity: color.rgb.a ? color.rgb.a : 1
                };
                setCurrentColor(newColor);
            };

            const convertCurrentColorToRGBA = () => {
                const rgb = tinycolor(currentColor.colors[0]).toRgb();
                rgb.a = currentColor.opacity;
                return rgb;
            };

            return (
                <div
                    css={css`
                        background-color: lightyellow;
                        border: 1px solid black;
                        width: 300px;
                        height: 350px;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                    `}
                >
                    <BloomSketchPicker
                        noAlphaSlider={true}
                        onChange={handlePickerChange}
                        color={convertCurrentColorToRGBA()}
                    />
                    <div
                        css={css`
                            background-color: ${currentColor.colors[0]};
                            width: 150px;
                            height: 22px;
                            padding: 5px;
                            margin-top: 20px;
                            text-align: center;
                        `}
                    >
                        Chosen color block
                    </div>
                </div>
            );
        })
    )
    .add("BloomSketchPicker with transparency", () =>
        React.createElement(() => {
            const initialColor = "#aa0000";
            const [currentColor, setCurrentColor] = useState<IColorInfo>({
                colors: [initialColor],
                opacity: 1
            });

            const convertCurrentColorToRGBA = () => {
                const rgb = tinycolor(currentColor.colors[0]).toRgb();
                rgb.a = currentColor.opacity;
                return rgb;
            };

            const getCurrentCssRgbaColor = () => {
                const rgb = convertCurrentColorToRGBA();
                return `rgba(${rgb.r}, ${rgb.g}, ${rgb.b}, ${rgb.a})`;
            };

            const handlePickerChange = (color: ColorResult) => {
                const newColor: IColorInfo = {
                    colors: [color.hex],
                    opacity: color.rgb.a ? color.rgb.a : 1
                };
                setCurrentColor(newColor);
            };
            const hexValueString = `Hex value = ${currentColor.colors[0]}`;
            const transparentValue = (1 - currentColor.opacity) * 100;
            const transparencyString = `Transparency = ${transparentValue.toFixed(
                0
            )}%`;

            return (
                <div
                    css={css`
                        background-color: lightyellow;
                        border: 1px solid black;
                        width: 300px;
                        height: 375px;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                    `}
                >
                    <BloomSketchPicker
                        noAlphaSlider={false}
                        onChange={handlePickerChange}
                        color={convertCurrentColorToRGBA()}
                    />
                    <div
                        css={css`
                            background-color: ${getCurrentCssRgbaColor()};
                            width: 150px;
                            height: 22px;
                            padding: 5px;
                            margin-top: 20px;
                            text-align: center;
                        `}
                    >
                        Chosen color block
                    </div>
                    <Typography>{hexValueString}</Typography>
                    <Typography>{transparencyString}</Typography>
                </div>
            );
        })
    )
    .add("Hex Color Input", () =>
        React.createElement(() => {
            const [currentColor, setCurrentColor] = useState<IColorInfo>({
                colors: ["#FEDCBA"],
                opacity: 1.0
            });

            const getColorInfoFromString = (color: string): IColorInfo => {
                return { colors: [color], opacity: 1.0 };
            };
            return (
                <div
                    style={mainBlockStyles}
                    css={css`
                        background-color: yellow;
                    `}
                >
                    <HexColorInput
                        initial={currentColor}
                        onChangeComplete={newValue => {
                            setCurrentColor(getColorInfoFromString(newValue));
                        }}
                    />
                    <div
                        css={css`
                            background-color: ${currentColor.colors[0]};
                            width: 150px;
                            height: 22px;
                            padding: 5px;
                            margin-top: 20px;
                            text-align: center;
                        `}
                    >
                        Chosen color block
                    </div>
                </div>
            );
        })
    );
