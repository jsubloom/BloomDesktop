/** @jsx jsx **/
import { jsx, css } from "@emotion/core";

import * as React from "react";
import { useState, useContext } from "react";
import {
    PreviewPanel,
    SettingsPanel,
    UnderPreviewPanel
} from "../commonPublish/PublishScreenBaseComponents";
import PublishScreenTemplate from "../commonPublish/PublishScreenTemplate";
import { DeviceAndControls } from "../commonPublish/DeviceAndControls";
import * as ReactDOM from "react-dom";
import ThemeProvider from "@material-ui/styles/ThemeProvider";
import { darkTheme, lightTheme } from "../../bloomMaterialUITheme";
import { StorybookContext } from "../../.storybook/StoryBookContext";
import {
    useSubscribeToWebSocketForStringMessage,
    useSubscribeToWebSocketForEvent
} from "../../utils/WebSocketManager";
import BloomButton from "../../react_components/bloomButton";
import { EPUBHelpGroup } from "./ePUBHelpGroup";
import { PWithLink } from "../../react_components/pWithLink";
import { EPUBSettingsGroup } from "./ePUBSettingsGroup";
import { PublishProgressDialog } from "../commonPublish/PublishProgressDialog";
import BookMetadataDialog from "../metadata/BookMetadataDialog";
import { useL10n } from "../../react_components/l10nHooks";
import { ProgressState } from "../commonPublish/PublishProgressDialogInner";
import { BloomApi } from "../../utils/bloomApi";
import { hookupLinkHandler } from "../../utils/linkHandler";
import { NoteBox } from "../../react_components/BloomDialog/commonDialogComponents";
import { P } from "../../react_components/l10nComponents";

export const EPUBPublishScreen = () => {
    // When the user changes some features, included languages, etc., we
    // need to rebuild the book and re-run all of our Bloom API queries.
    // This requires a hard-reset of the whole screen, which we do by
    // incrementing a `key` prop on the core of this screen.
    const [keyForReset, setKeyForReset] = useState(0);

    // When the user changes epub mode (and other settings), the only way to see
    // the updated epub is to click the Refresh button.  When this happens, the
    // mode value is preserved across the refresh, but users would like to see the
    // new value during the refresh period, which can take several seconds.
    // Preserving this value requires saving it at this level where values are
    // not destroyed during refresh.  See comments toward the end of
    // https://issues.bloomlibrary.org/youtrack/issue/BL-11043.
    const defaultEpubModeRef = React.useRef("fixed");
    const [epubMode, setEpubmode] = BloomApi.useApiStringState(
        "publish/epub/epubMode",
        defaultEpubModeRef.current
    );

    return (
        <EPUBPublishScreenInternal
            key={keyForReset}
            onReset={() => {
                setKeyForReset(keyForReset + 1);
            }}
            epubMode={epubMode}
            setEpubMode={(mode: string) => {
                setEpubmode(mode);
                defaultEpubModeRef.current = mode;
            }}
        />
    );
};

const EPUBPublishScreenInternal: React.FunctionComponent<{
    onReset: () => void;
    epubMode: string;
    setEpubMode: (mode: string) => void;
}> = props => {
    const inStorybookMode = useContext(StorybookContext);
    const [closePending, setClosePending] = useState(false);
    const [highlightRefresh, setHighlightRefresh] = useState(false);
    const [progressState, setProgressState] = useState(ProgressState.Working);
    React.useEffect(() => hookupLinkHandler(), []);
    const [bookUrl, setBookUrl] = useState(
        inStorybookMode
            ? window.location.protocol +
                  "//" +
                  window.location.host +
                  "/templates/Sample Shells/The Moon and the Cap" // Enhance: provide an actual epub in the source tree
            : "" // otherwise, wait for the websocket to deliver a url when the c# has finished creating the epub
    );

    const [landscape] = BloomApi.useApiBoolean("publish/epub/landscape", false);

    useSubscribeToWebSocketForEvent(
        "publish-epub",
        "startingEbookCreation",
        e => {
            setProgressState(ProgressState.Working);
        }
    );

    // The c# api responds to changes of settings by auto-starting a new epub build. When
    // it is done, it calls this (but actually the same url, alas).
    useSubscribeToWebSocketForStringMessage(
        "publish-epub",
        "newEpubReady",
        url => {
            // add a random component so that react will reload the iframe
            setBookUrl(url + "&random=" + Math.random().toString());
            setClosePending(true);
            setHighlightRefresh(false);
        }
    );
    const isLicenseOK = BloomApi.useWatchBooleanEvent(
        true,
        "publish-epub",
        "publish/licenseOK"
    );

    const mainPanel = (
        <div className="ePUBPublishScreen">
            <PreviewPanel>
                <ThemeProvider theme={darkTheme}>
                    <DeviceAndControls
                        defaultLandscape={landscape}
                        canRotate={false}
                        url={bookUrl}
                        showRefresh={true}
                        highlightRefreshIcon={highlightRefresh}
                        onRefresh={() => props.onReset()}
                    />
                </ThemeProvider>
            </PreviewPanel>
            <UnderPreviewPanel>
                <div
                    css={css`
                        display: flex;
                    `}
                >
                    <BloomButton
                        css={css`
                            align-self: flex-start; // without this, it grows to the width of the column
                            width: 100px;
                            margin-inline-end: 50px !important; // !important needed to override material button base
                        `}
                        enabled={isLicenseOK}
                        clickApiEndpoint={"publish/epub/save"}
                        hasText={true}
                        l10nKey="PublishTab.Save"
                    >
                        Save...
                    </BloomButton>
                    <NoteBox
                        addBorder={true}
                        css={css`
                            width: 430px;
                        `}
                    >
                        <div
                            css={css`
                                > p:first-child {
                                    margin-top: 0;
                                }
                                > p:last-child {
                                    margin-bottom: 0;
                                }
                                ul {
                                    padding-inline-start: 0;
                                }
                                ul li {
                                    margin-left: 1.1em;
                                }
                            `}
                        >
                            <P
                                l10nKey="PublishTab.Epub.Notes"
                                css={css`
                                    font-weight: bold;
                                `}
                            >
                                Notes:
                            </P>
                            <ul>
                                <li>
                                    <PWithLink
                                        href="https://docs.bloomlibrary.org/ePUB-notes/"
                                        l10nKey="PublishTab.Epub.ReaderRecommendations"
                                        l10nComment="The text inside the [square brackets] will become a link to a website."
                                    >
                                        Most ePUB readers are very low quality
                                        ([see our research and
                                        recommendations]).
                                    </PWithLink>
                                </li>
                                <li>
                                    <PWithLink
                                        href="/bloom/api/help/Concepts/EPUB.htm"
                                        l10nKey="PublishTab.Epub.FeatureLimitations"
                                        l10nComment="The text inside the [square brackets] will become a link to a website."
                                    >
                                        [Some Bloom features] are not supported
                                        by most or all ePUB readers.
                                    </PWithLink>
                                </li>
                            </ul>
                            <P l10nKey="PublishTab.Epub.ConsiderBloomPUB">
                                Consider whether you can distribute your book
                                using the BloomPUB format instead of or in
                                addition to ePUB.
                            </P>
                        </div>
                    </NoteBox>
                </div>
            </UnderPreviewPanel>
        </div>
    );

    const optionsPanel = (
        <SettingsPanel>
            <EPUBSettingsGroup
                onChange={() => setHighlightRefresh(true)}
                mode={props.epubMode}
                setMode={props.setEpubMode}
            />
            {/* push everything below this to the bottom */}
            <div
                css={css`
                    margin-top: auto;
                `}
            />
            <EPUBHelpGroup />
        </SettingsPanel>
    );

    return (
        <React.Fragment>
            <PublishScreenTemplate
                bannerTitleEnglish="Publish as ePUB"
                bannerTitleL10nId="PublishTab.Epub.BannerTitle"
                bannerDescriptionMarkdown="Make an electronic book that can be read in EPUB readers on all devices."
                bannerDescriptionL10nId="PublishTab.Epub.BannerDescription"
                optionsPanelContents={optionsPanel}
            >
                {mainPanel}
            </PublishScreenTemplate>
            <PublishProgressDialog
                heading={useL10n("Creating ePUB", "PublishTab.Epub.Creating")}
                webSocketClientContext="publish-epub"
                startApiEndpoint="publish/epub/updatePreview"
                progressState={progressState}
                setProgressState={setProgressState}
                closePending={closePending}
                setClosePending={setClosePending}
            />
            <BookMetadataDialog />
        </React.Fragment>
    );
};

// a bit goofy... currently the html loads everything in publishUIBundlejs. So all the publish screens
// get any not-in-a-class code called, including ours. But it only makes sense to get wired up
// if that html has the root page we need.
if (document.getElementById("ePUBPublishScreen")) {
    ReactDOM.render(
        <ThemeProvider theme={lightTheme}>
            <EPUBPublishScreen />
        </ThemeProvider>,
        document.getElementById("ePUBPublishScreen")
    );
}
