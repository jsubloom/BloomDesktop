/** @jsx jsx **/
import { jsx, css } from "@emotion/react";
import * as React from "react";
import { useEffect, useRef, useState } from "react";
import { TextField, Step, StepLabel, StepContent } from "@mui/material";
import {
    get,
    getBloomApiPrefix,
    getBoolean,
    post,
    postString
} from "../../utils/bloomApi";
import { kBloomRed } from "../../utils/colorUtils";
import { BloomStepper } from "../../react_components/BloomStepper";
import { Div, P, Span } from "../../react_components/l10nComponents";
import BloomButton from "../../react_components/bloomButton";
import { PWithLink } from "../../react_components/pWithLink";
import {
    ProgressBox,
    ProgressBoxHandle
} from "../../react_components/Progress/progressBox";
import { MuiCheckbox } from "../../react_components/muiCheckBox";
import { useL10n } from "../../react_components/l10nHooks";
import { kWebSocketContext } from "./LibraryPublishScreen";
import {
    useSubscribeToWebSocketForObject,
    useSubscribeToWebSocketForStringMessage
} from "../../utils/WebSocketManager";
import { Link } from "../../react_components/link";
import {
    DialogResult,
    ConfirmDialog,
    showConfirmDialog
} from "../../react_components/confirmDialog";
import { BloomSplitButton } from "../../react_components/bloomSplitButton";

interface IReadonlyBookInfo {
    title: string;
    copyright: string;
    license: string;
    licenseType: string;
    licenseToken: string;
    licenseRights: string;
    isTemplate: boolean;
}

const kWebSocketEventId_uploadSuccessful: string = "uploadSuccessful";

export const LibraryPublishSteps: React.FunctionComponent = () => {
    const localizedSummary = useL10n("Summary", "PublishTab.Upload.Summary");
    const localizedAllRightsReserved = useL10n(
        "All rights reserved (Contact the Copyright holder for any permissions.)",
        "PublishTab.Upload.AllReserved"
    );
    const localizedSuggestChangeCC = useL10n(
        "Suggestion: Creative Commons Licenses make it much easier for others to use your book, even if they aren't fluent in the language of your custom license.",
        "PublishTab.Upload.SuggestChangeCC"
    );
    const localizedSuggestAssignCC = useL10n(
        "Suggestion: Assigning a Creative Commons License makes it easy for you to clearly grant certain permissions to everyone.",
        "PublishTab.Upload.SuggestAssignCC"
    );
    const localizedUploadBook = useL10n(
        "Upload Book",
        "PublishTab.Upload.UploadButton"
    );
    const localizedUploadCollection = useL10n(
        "Upload this Collection",
        "PublishTab.Upload.UploadCollection"
    );
    const localizedUploadFolder = useL10n(
        "Upload Folder of Collections",
        "PublishTab.Upload.UploadFolder"
    );

    const progressBoxRef = useRef<ProgressBoxHandle>(null);

    const [bookInfo, setBookInfo] = useState<IReadonlyBookInfo>();
    useEffect(() => {
        get("libraryPublish/getBookInfo", result => {
            setBookInfo(result.data);
            setSummary(result.data.summary);
        });
    }, []);
    const [useSandbox, setUseSandbox] = useState<boolean>(false);
    const [uploadButtonText, setUploadButtonText] = useState<string>(
        localizedUploadBook
    );
    useEffect(() => {
        getBoolean("libraryPublish/useSandbox", setUseSandbox);
    }, []);
    useEffect(() => {
        setUploadButtonText(
            localizedUploadBook +
                (useSandbox ? " (to dev.bloomlibrary.org)" : "")
        );
    }, [useSandbox, localizedUploadBook]);

    const [summary, setSummary] = useState<string>("");
    useEffect(() => {
        if (bookInfo) postString("libraryPublish/setSummary", summary);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [summary]); // purposefully not including bookInfo, so we don't post on initial load

    function isReadyForAgreements(): boolean {
        return !!bookInfo?.title && !!bookInfo?.copyright;
    }
    const [agreementsAccepted, setAgreementsAccepted] = useState<boolean>(
        false
    );
    function isReadyForUpload(): boolean {
        return isReadyForAgreements() && agreementsAccepted;
    }

    function confirmWithUserIfNecessaryAndUpload() {
        if (bookInfo?.isTemplate) {
            showConfirmDialog();
        } else {
            uploadOneBook();
        }
    }

    const [isUploading, setIsUploading] = useState<boolean>(false);
    function uploadOneBook() {
        setIsUploading(true);
        post("libraryPublish/upload");
    }

    function bulkUploadCollection() {
        post("libraryPublish/uploadCollection");
    }
    function bulkUploadFolderOfCollections() {
        // Nothing to do either on success or failure, including possible timeout,
        // or the user canceling. This is because the "result" comes back
        // via a websocket that sets the new result (just below). This approach is needed because otherwise
        // the browser would time out while waiting for the user to finish using the system folder-choosing dialog.
        post("common/chooseFolder");
    }
    useSubscribeToWebSocketForObject<{ success: boolean; path: string }>(
        "common",
        "chooseFolder-results",
        results => {
            if (results.success) {
                postString(
                    "libraryPublish/uploadFolderOfCollections",
                    results.path
                );
            }
        }
    );

    const [bookUrl, setBookUrl] = useState<string>("");

    // When C# finishes the upload, it calls this.
    useSubscribeToWebSocketForStringMessage(
        kWebSocketContext,
        kWebSocketEventId_uploadSuccessful,
        url => {
            setIsUploading(false);
            setBookUrl(url);
            setIsUploadComplete(true);
        }
    );

    const [isUploadComplete, setIsUploadComplete] = useState<boolean>(false);

    const [licenseBlock, setLicenseBlock] = useState<JSX.Element>(
        <React.Fragment />
    );
    useEffect(() => {
        switch (bookInfo?.licenseType) {
            case "CreativeCommons":
                setLicenseBlock(
                    <img
                        src={`${getBloomApiPrefix()}copyrightAndLicense/ccImage?token=${bookInfo?.licenseToken?.toLowerCase()}`}
                        css={css`
                            width: 100px;
                        `}
                    />
                );
                break;
            case "Null":
                setLicenseBlock(
                    <div>
                        <div>{localizedAllRightsReserved}</div>
                        <WarningMessage>
                            {localizedSuggestAssignCC}
                        </WarningMessage>
                    </div>
                );
                break;
            case "Custom":
                setLicenseBlock(
                    <div>
                        <div>{bookInfo?.licenseRights}</div>
                        <WarningMessage>
                            {localizedSuggestChangeCC}
                        </WarningMessage>
                    </div>
                );
                break;
        }
    }, [
        bookInfo,
        localizedAllRightsReserved,
        localizedSuggestAssignCC,
        localizedSuggestChangeCC
    ]);

    return (
        <React.Fragment>
            <BloomStepper orientation="vertical">
                <Step active={true} completed={isReadyForAgreements()}>
                    <StepLabel>
                        <Span l10nKey="PublishTab.Upload.ConfirmMetadata">
                            Confirm Metadata
                        </Span>
                    </StepLabel>
                    <StepContent>
                        <div
                            css={css`
                                font-size: larger;
                            `}
                        >
                            <div
                                css={css`
                                    font-weight: bold;
                                `}
                            >
                                {bookInfo?.title || (
                                    <MissingInfo
                                        text="Title Missing"
                                        l10nKey={
                                            "PublishTab.Upload.Missing.Title"
                                        }
                                    />
                                )}
                            </div>
                            <div>
                                {bookInfo?.copyright || (
                                    <MissingInfo
                                        text="Copyright Missing"
                                        l10nKey={
                                            "PublishTab.Upload.Missing.Copyright"
                                        }
                                    />
                                )}
                            </div>
                            {licenseBlock}
                        </div>
                        <TextField
                            // needed by aria for a11y
                            id="book summary"
                            value={summary}
                            onChange={e => setSummary(e.target.value)}
                            label={localizedSummary}
                            margin="normal"
                            variant="outlined"
                            InputLabelProps={{
                                shrink: true
                            }}
                            multiline
                            rows="2"
                            aria-label="Book summary"
                            fullWidth
                            css={css`
                                margin-left: -15px; // Align the label with the read-only data labels. Determined experimentally.
                                margin-top: 24px;

                                // This is messy. MUI doesn't seem to let you easily (and correctly) change the label size.
                                // You're supposed to be able to set a style on InputLabelProps and set fontSize, but then
                                // the border around the textbox partially goes through it.
                                // The way that break in the border is implemented is a "legend" which obscures the border.
                                // The legend has the same text as the label. So we have to make the text the same size.
                                // The original transform is translate(14px, -9px) scale(1). In order to make "larger" match,
                                // we unscale it here -- scale(1), and as a result we have to increase the scale of the legend.
                                .MuiInputLabel-root {
                                    color: inherit;
                                    font-weight: 500;
                                    font-size: larger;
                                    transform: translate(14px, -9px) scale(1);
                                    &.Mui-focused {
                                        color: inherit;
                                    }
                                }
                                legend {
                                    font-weight: 500;
                                    font-size: larger;
                                    transform: scale(1.5);
                                }
                            `}
                        />
                    </StepContent>
                </Step>
                <Step
                    active={isReadyForAgreements()}
                    expanded={true}
                    disabled={!isReadyForAgreements()}
                    completed={isReadyForUpload()}
                >
                    <StepLabel>
                        <Span l10nKey="PublishTab.Upload.Agreements">
                            Agreements
                        </Span>
                    </StepLabel>
                    <StepContent>
                        <Agreements
                            disabled={!isReadyForAgreements()}
                            onReadyChange={setAgreementsAccepted}
                        />
                    </StepContent>
                </Step>
                <Step
                    active={isReadyForUpload()}
                    expanded={true}
                    disabled={!isReadyForUpload()}
                    completed={isUploadComplete}
                >
                    <StepLabel>
                        <Span l10nKey={"Common.Upload"}>Upload</Span>
                    </StepLabel>
                    <StepContent>
                        {/* This will move to the settings section
                        <MuiCheckbox
                        label={
                            <React.Fragment>
                                <img src="/bloom/publish/LibraryPublish/DRAFT-Stamp.svg" />
                                <Span l10nKey="PublishTab.Upload.Draft">
                                    Show this book only to reviewers with whom I
                                    share the URL of this book.
                                </Span>
                            </React.Fragment>
                        }
                        checked={false} //TODO
                        onCheckChanged={newValue => {
                            //TODO
                        }}
                        disabled={!isReadyForUpload()}
                    /> */}
                        <div
                            css={css`
                                display: flex;
                                justify-content: space-between;
                                margin-top: 8px;
                            `}
                        >
                            {isUploading ? (
                                <BloomButton
                                    enabled={true}
                                    l10nKey={"Common.Cancel"}
                                    onClick={() => {
                                        setIsUploading(false);
                                        post("libraryPublish/cancel");
                                    }}
                                >
                                    Cancel
                                </BloomButton>
                            ) : (
                                <BloomSplitButton
                                    disabled={!isReadyForUpload()}
                                    options={[
                                        {
                                            english: uploadButtonText,
                                            l10nId: "already-localized",
                                            onClick: () => {
                                                progressBoxRef.current?.clear();
                                                confirmWithUserIfNecessaryAndUpload();
                                            }
                                        },
                                        {
                                            english: localizedUploadCollection,
                                            l10nId: "already-localized",
                                            requiresEnterpriseSubscription: true,
                                            onClick: () => {
                                                progressBoxRef.current?.clear();
                                                bulkUploadCollection();
                                            }
                                        },
                                        {
                                            english: localizedUploadFolder,
                                            l10nId: "already-localized",
                                            requiresEnterpriseSubscription: true,
                                            onClick: () => {
                                                progressBoxRef.current?.clear();
                                                bulkUploadFolderOfCollections();
                                            }
                                        }
                                    ]}
                                ></BloomSplitButton>
                            )}
                            <BloomButton
                                variant="text"
                                enabled={isReadyForUpload()}
                                l10nKey={"PublishTab.Upload.SignIn"}
                            >
                                Sign in or sign up to Bloomlibrary.org
                            </BloomButton>
                        </div>
                        <div
                            css={css`
                                margin-top: 16px;
                            `}
                        >
                            <Div l10nKey={"PublishTab.Upload.UploadProgress"}>
                                Upload Progress
                            </Div>
                            <ProgressBox
                                ref={progressBoxRef}
                                webSocketContext={kWebSocketContext}
                                css={css`
                                    height: 200px;
                                `}
                            ></ProgressBox>
                        </div>
                    </StepContent>
                </Step>
                <Step
                    active={isUploadComplete}
                    expanded={isUploadComplete}
                    disabled={!isUploadComplete}
                >
                    <StepLabel>
                        <Span l10nKey="PublishTab.Upload.TestBook">
                            Test out your Book
                        </Span>
                    </StepLabel>
                    <StepContent>
                        <PWithLink
                            l10nKey={"PublishTab.Upload.TestBook.Text1"}
                            href={bookUrl}
                        >
                            Here is [your new page] on Bloom Library. We will
                            soon process your book into various formats and add
                            them to this page. Check back in about 10 minutes.
                            If we encounter any problems, that page will tell
                            you about them.
                        </PWithLink>
                        <P l10nKey={"PublishTab.Upload.TestBook.Text2"}>
                            If you make changes to this book, you can return
                            here to upload it again. Your new version will just
                            replace the existing one.
                        </P>
                    </StepContent>
                </Step>
            </BloomStepper>
            <ConfirmDialog
                title="Warning"
                titleL10nKey="Warning"
                message={
                    "This book seems to be a template, that is, it contains blank pages for authoring a new book " +
                    "rather than content to translate into other languages. " +
                    "If that is not what you intended, you should get expert help before uploading this book." +
                    "\n\n" +
                    "Do you want to go ahead?"
                }
                messageL10nKey="PublishTab.Upload.Template"
                confirmButtonLabel="Yes"
                confirmButtonLabelL10nKey="Common.Yes"
                cancelButtonLabel="No"
                cancelButtonLabelL10nKey="Common.No"
                onDialogClose={function(result: DialogResult): void {
                    if (result === DialogResult.Confirm) uploadOneBook();
                }}
            />
        </React.Fragment>
    );
};

// remaining from JH's original TODO list:
// (some of these apply to the Settings)
// Choose languages to upload
// Really hook up login/signup
// Disable Upload until all done
// Features

const Agreements: React.FunctionComponent<{
    disabled: boolean;
    onReadyChange: (v: boolean) => void;
}> = props => {
    const totalCheckboxes = 3;
    const [numChecked, setNumChecked] = useState<number>(0);
    useEffect(() => {
        props.onReadyChange(numChecked === totalCheckboxes);
    }, [numChecked]);
    function handleChange(isChecked: boolean) {
        setNumChecked(prevNumChecked =>
            isChecked ? prevNumChecked + 1 : prevNumChecked - 1
        );
    }
    return (
        <React.Fragment>
            <AgreementCheckbox
                label={
                    <React.Fragment>
                        <Span l10nKey="PublishTab.Upload.Agreement.PermissionToPublish">
                            I have permission to publish all the text and images
                            in this book.
                        </Span>{" "}
                        <Link href={"TODO"} l10nKey="Common.LearnMore">
                            Learn More
                        </Link>
                    </React.Fragment>
                }
                disabled={props.disabled}
                onChange={checked => handleChange(checked)}
            />
            <AgreementCheckbox
                label={
                    <Span l10nKey={"PublishTab.Upload.Agreement.GivesCredit"}>
                        The book gives credit to the the author, translator, and
                        illustrator(s).
                    </Span>
                }
                disabled={props.disabled}
                onChange={checked => handleChange(checked)}
            />
            <AgreementCheckbox
                label={
                    <PWithLink
                        href={"https://bloomlibrary.org/terms"}
                        l10nKey={"PublishTab.Upload.Agreement.AgreeToTerms"}
                        css={css`
                            // We don't want normal padding the browser adds, mostly so the height matches the other checkboxes.
                            margin: 0;
                        `}
                    >
                        I agree to the [Bloom Library Terms of Use].
                    </PWithLink>
                }
                disabled={props.disabled}
                onChange={checked => handleChange(checked)}
            />
        </React.Fragment>
    );
};

// This component is a bit odd. It doesn't quite fit into controlled or uncontrolled.
// We have an onChange handler because we need to know when its state changes.
// But we never pass in the value it because always starts in a certain state (unchecked/false).
// We can do this because it is only designed to be used in this limited context.
const AgreementCheckbox: React.FunctionComponent<{
    label: string | React.ReactNode;
    disabled: boolean;
    onChange: (v: boolean) => void;
}> = props => {
    const [isChecked, setIsChecked] = useState(false);
    function handleCheckChanged(isChecked: boolean) {
        setIsChecked(isChecked);
        props.onChange(isChecked);
    }
    return (
        <div>
            <MuiCheckbox
                label={props.label}
                checked={isChecked}
                onCheckChanged={newState => {
                    handleCheckChanged(!!newState);
                }}
                disabled={props.disabled}
            ></MuiCheckbox>
        </div>
    );
};

const WarningMessage: React.FunctionComponent = props => {
    return (
        <div
            css={css`
                font-size: small;
                color: ${kBloomRed};
            `}
        >
            {props.children}
        </div>
    );
};

const MissingInfo: React.FunctionComponent<{
    text: string;
    l10nKey: string;
}> = props => {
    return (
        <Div
            l10nKey={props.l10nKey}
            css={css`
                font-size: unset;
                font-weight: normal;
                color: ${kBloomRed};
            `}
        >
            {props.text}
        </Div>
    );
};
