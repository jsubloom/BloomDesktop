/** @jsx jsx **/
import { jsx, css } from "@emotion/react";
import * as React from "react";
import { useState, useContext, useEffect, useRef } from "react";

import {
    PreviewPanel,
    HelpGroup,
    SettingsPanel,
    PreviewPublishPanel
} from "../commonPublish/PublishScreenBaseComponents";
import { PDFPrintFeaturesGroup } from "./PDFPrintFeaturesGroup";
import PublishScreenTemplate from "../commonPublish/PublishScreenTemplate";
import ReactDOM = require("react-dom");
import { ThemeProvider, StyledEngineProvider } from "@mui/material/styles";
import { darkTheme, lightTheme } from "../../bloomMaterialUITheme";
import { useL10n } from "../../react_components/l10nHooks";
import Typography from "@mui/material/Typography";
import Button from "@mui/material/Button";
import { CircularProgress } from "@mui/material";
import { getString, post, useWatchString } from "../../utils/bloomApi";

import { Div, Span } from "../../react_components/l10nComponents";
import { ApiCheckbox } from "../../react_components/ApiCheckbox";
import {
    DialogCancelButton,
    DialogOkButton
} from "../../react_components/BloomDialog/commonDialogComponents";
import {
    BloomDialog,
    DialogBottomButtons,
    DialogMiddle
} from "../../react_components/BloomDialog/BloomDialog";
import { ProgressDialog } from "../../react_components/Progress/ProgressDialog";

// The common behavior of the Print and Save buttons.
// There is probably some way to get this look out of BloomButton,
// but it seems more trouble than it's worth.
const PrintSaveButton: React.FunctionComponent<{
    onClick: () => void;
    label: string;
    l10nId: string;
    imgSrc: string;
    enabled: boolean;
}> = props => {
    const label = useL10n(props.label, props.l10nId);
    return (
        <Button onClick={props.onClick} disabled={!props.enabled}>
            <img src={props.imgSrc} />
            <div
                css={css`
                    width: 0.5em;
                `}
            />
            <span
                css={css`
                    color: black;
                    opacity: ${props.enabled ? "100%" : "38%"};
                    text-transform: none !important;
                `}
            >
                {label}
            </span>
        </Button>
    );
};

export const PDFPrintPublishScreen = () => {
    const [path, setPath] = useState("");
    const [printSettings, setPrintSettings] = useState("");
    // We need a ref to the real DOM object of the iframe that holds the print preview
    // so we can actually tell it to print.
    const iframeRef = useRef<HTMLIFrameElement>(null);

    const progressHeader = useL10n("Progress", "Common.Progress");
    const showProgress = useRef<() => void | undefined>();
    const closeProgress = useRef<() => void | undefined>();

    const mainPanel = (
        <React.Fragment>
            <PreviewPanel
                // This panel has a black background. If it is visible, it looks odd combined with
                // the grey background (which we can't change) that WebView2 shows when previewing a PDF.
                css={css`
                    padding: 0;
                `}
            >
                <StyledEngineProvider injectFirst>
                    <ThemeProvider theme={darkTheme}>
                        {path ? (
                            <iframe
                                ref={iframeRef}
                                css={css`
                                    height: 100%;
                                    width: 100%;
                                `}
                                src={path}
                            />
                        ) : (
                            <Typography
                                css={css`
                                    color: white;
                                    align-self: center;
                                    margin-left: 20px;
                                `}
                            >
                                <Span l10nKey="PublishTab.PdfMaker.ClickToStart">
                                    "Click a button on the right to start
                                    creating PDF."
                                </Span>
                            </Typography>
                        )}
                    </ThemeProvider>
                </StyledEngineProvider>
            </PreviewPanel>
            <PreviewPublishPanel
                css={css`
                    display: block;
                    flex-grow: 1;
                `}
            ></PreviewPublishPanel>
        </React.Fragment>
    );

    const optionsPanel = (
        <SettingsPanel>
            <PDFPrintFeaturesGroup
                onChange={() => {
                    showProgress.current?.();
                }}
                onGotPdf={path => {
                    setPath(path);
                    closeProgress.current?.();
                }}
            />
            {/* push everything to the bottom */}
            <div
                css={css`
                    margin-top: auto;
                `}
            />
            <HelpGroup>
                <Typography>Not a real "HelpGroup"; needs changing</Typography>
                {/* Replace with links to PDF and Printing help
                <HelpLink
                    l10nKey="PublishTab.Android.AboutBloomPUB"
                    helpId="Tasks/Publish_tasks/Make_a_BloomPUB_file_overview.htm"
                >
                    About BloomPUB
                </HelpLink>
                */}
            </HelpGroup>
        </SettingsPanel>
    );

    const printNow = () => {
        if (iframeRef.current) {
            iframeRef.current.contentWindow?.print();
            // Unfortunately, we have no way to know whether the user really
            // printed, or canceled in the browser print dialog.
            post("publish/pdf/printAnalytics");
        }
    };

    const handlePrint = () => {
        getString("publish/pdf/printSettingsPath", instructions => {
            if (instructions) {
                // This causes the instructions image to be displayed along with a dialog
                // in which the user can continue (or set a checkbox to prevent this
                // happening again.)
                setPrintSettings(instructions);
            } else {
                printNow();
            }
        });
    };

    const rightSideControls = (
        <React.Fragment>
            <PrintSaveButton
                onClick={handlePrint}
                enabled={!!path}
                l10nId="PublishTab.PrintButton"
                imgSrc="./Print.png"
                label="Print..."
            />
            <PrintSaveButton
                onClick={() => {
                    post("publish/pdf/save");
                }}
                enabled={!!path}
                l10nId="PublishTab.SaveButton"
                imgSrc="./Usb.png"
                label="Save PDF..."
            />
        </React.Fragment>
    );

    return (
        <React.Fragment>
            <PublishScreenTemplate
                bannerTitleEnglish="Publish to PDF &amp; Print"
                bannerTitleL10nId="PublishTab.PdfPrint.BannerTitle"
                bannerRightSideControls={rightSideControls}
                optionsPanelContents={optionsPanel}
            >
                {mainPanel}
            </PublishScreenTemplate>

            <ProgressDialog
                title={progressHeader}
                determinate={true}
                size="small"
                showCancelButton={true}
                onCancel={() => {
                    post("publish/pdf/cancel");
                    closeProgress.current?.();
                    setPath("");
                }}
                setShowDialog={showFunc => (showProgress.current = showFunc)}
                setCloseDialog={closeFunc =>
                    (closeProgress.current = closeFunc)
                }
            />

            <BloomDialog
                open={!!printSettings}
                // eslint-disable-next-line @typescript-eslint/no-empty-function
                onClose={() => {}}
            >
                <DialogMiddle>
                    <Div l10nKey="SamplePrintNotification.PleaseNotice">
                        Please notice the sample printer settings below. Use
                        them as a guide while you set up the printer.
                    </Div>
                    <ApiCheckbox
                        english="I get it. Do not show this again."
                        l10nKey="SamplePrintNotification.IGetIt"
                        apiEndpoint="publish/pdf/dontShowSamplePrint"
                    ></ApiCheckbox>
                </DialogMiddle>
                <DialogBottomButtons>
                    <DialogOkButton
                        onClick={() => {
                            printNow();
                            // This is unfortunate. It will hide not only this dialog but the image that
                            // shows how to set things. The call to printNow will initially show the dialog that
                            // the print settings are supposed to help with. We'd like to have it
                            // visible until the user clicks Print or Cancel. But
                            // - We can't find any way to find out when the user clicks Print or Cancel,
                            //   so we'd have to leave it up to the user to click some Close control to get rid
                            //   of the settings (we could of course get rid of this dialog right away).
                            // - The print dialog occupies more-or-less the entire WebView2 control; there's
                            //   nowhere left to show the recommendations.
                            // So we just have to hope the user can remember them.
                            setPrintSettings("");
                        }}
                    ></DialogOkButton>
                </DialogBottomButtons>
            </BloomDialog>
            <div
                css={css`
                    position: absolute;
                    bottom: 0;
                    right: 0;
                    display: ${printSettings ? "block" : "none"};
                `}
            >
                <img src={printSettings} />
            </div>
            {/* In storybook, there's no bloom backend to run the progress dialog */}
            {/* {inStorybookMode || (
                <PublishProgressDialog
                    heading={heading}
                    startApiEndpoint="publish/android/updatePreview"
                    webSocketClientContext="publish-android"
                    progressState={progressState}
                    setProgressState={setProgressState}
                    closePending={closePending}
                    setClosePending={setClosePending}
                    onUserStopped={() => {
                        postData("publish/android/usb/stop", {});
                        postData("publish/android/wifi/stop", {});
                        setClosePending(true);
                    }}
                />
            )} */}
        </React.Fragment>
    );
};

// a bit goofy... currently the html loads everything in publishUIBundlejs. So all the publish screens
// get any not-in-a-class code called, including ours. But it only makes sense to get wired up
// if that html has the root page we need.
if (document.getElementById("PdfPrintPublishScreen")) {
    ReactDOM.render(
        <StyledEngineProvider injectFirst>
            <ThemeProvider theme={lightTheme}>
                <PDFPrintPublishScreen />
            </ThemeProvider>
        </StyledEngineProvider>,
        document.getElementById("PdfPrintPublishScreen")
    );
}
