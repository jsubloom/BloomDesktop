/** @jsx jsx **/
import { jsx, css } from "@emotion/core";

import * as React from "react";
import { BloomApi } from "../utils/bloomApi";
import { Div, P, Span } from "../react_components/l10nComponents";
import BloomButton from "../react_components/bloomButton";

import {
    BloomDialog,
    DialogBottomButtons,
    DialogBottomLeftButtons,
    DialogMiddle,
    DialogTitle,
    IBloomDialogEnvironmentParams,
    useSetupBloomDialog
} from "../react_components/BloomDialog/BloomDialog";
import { useL10n } from "../react_components/l10nHooks";
import {
    DialogCancelButton,
    DialogReportButton,
    ErrorBox,
    NoteBox
} from "../react_components/BloomDialog/commonDialogComponents";
import { WireUpForWinforms } from "../utils/WireUpWinform";

// Six variations are all handled here.
enum JoinCollectionState {
    // there is no local collection with the same name. Offer to create one.
    "CreateNewCollection",
    // there is a local collection with the same name that is not a TC. Offer to merge.
    "MatchesExistingNonTeamCollection",
    // we are already joined to a matching TC (same collection ID and already points here). Offer to open.
    "MatchesExistingTeamCollection",
    // there is an existing local collection of the same name joined to this TC (same ID) but at another location. Offer to switch.
    "MatchesExistingTeamCollectionElsewhere",
    // there is an existing local collection of the same name joined to another TC (different ID). Offer to report.
    "MatchesDifferentTeamCollection",
    // The JoinCollection file is lost...other necessary parts of the TC are missing.
    "IncompleteTeamCollection"
}

// In normal use (not storybook), this is a top-level component in a ReactDialog.
// The props are set in C# and passed to the ReactDialog constructor in FolderTeamCollection.ShowJoinCollectionTeamDialog()

export const JoinTeamCollectionDialog: React.FunctionComponent<{
    missingTcPieces?: string;
    collectionName: string;
    existingCollection: boolean;
    isAlreadyTcCollection: boolean;
    isSameCollection: boolean;
    isCurrentCollection: boolean; // that is, it already points at the one we are joining
    existingCollectionFolder: string; // if there's an existing local collection, a path to it
    conflictingCollection: string; // if there's a conflicting repo that the existing collection is connected to, a path to it.
    joiningRepo?: string; // the repo we're trying to join
    joiningGuid?: string;
    localGuid?: string;
    dialogEnvironment?: IBloomDialogEnvironmentParams;
}> = props => {
    const {
        showDialog,
        closeDialog,
        propsForBloomDialog
    } = useSetupBloomDialog(props.dialogEnvironment);

    const dialogTitle = useL10n(
        'Join the Bloom Team Collection "%0"',
        "TeamCollection.JoinHeading",
        undefined,
        props.collectionName,
        undefined,
        true // temporarilyDisableI18nWarning
    );
    const dialogState = getDialogStateFromProps();
    const l10nJoinButtonKey = getL10nKeyForJoinButton();
    const joinButtonEnglish = getJoinButtonEnglish();

    function getDialogStateFromProps(): JoinCollectionState {
        if (props.missingTcPieces) {
            return JoinCollectionState.IncompleteTeamCollection;
        }
        if (!props.existingCollection) {
            return JoinCollectionState.CreateNewCollection;
        }
        if (!props.isAlreadyTcCollection) {
            return JoinCollectionState.MatchesExistingNonTeamCollection;
        }
        if (!props.isSameCollection) {
            return JoinCollectionState.MatchesDifferentTeamCollection;
        }
        if (props.isCurrentCollection) {
            return JoinCollectionState.MatchesExistingTeamCollection;
        } else {
            return JoinCollectionState.MatchesExistingTeamCollectionElsewhere;
        }
    }

    function getL10nKeyForJoinButton(): string {
        return dialogState ===
            JoinCollectionState.MatchesExistingNonTeamCollection
            ? "TeamCollection.JoinAndMerge"
            : dialogState === JoinCollectionState.MatchesExistingTeamCollection
            ? "TeamCollection.Open"
            : "TeamCollection.Join";
    }

    function getJoinButtonEnglish(): string {
        return dialogState ===
            JoinCollectionState.MatchesExistingNonTeamCollection
            ? "Join and Merge"
            : dialogState === JoinCollectionState.MatchesExistingTeamCollection
            ? "Open"
            : "Join";
        // Leaving it as "join" for the pathological cases, though it will be disabled.
    }

    function getBloomWillSetYouUp() {
        return (
            <P
                l10nKey="TeamCollection.Joining"
                l10nParam0={props.collectionName}
                temporarilyDisableI18nWarning={true}
            >
                Bloom will set you up to work together with your team on this
                collection of books.
            </P>
        );
    }

    function getDialogBodyExistingNonTC(): JSX.Element {
        return (
            <React.Fragment>
                {getBloomWillSetYouUp()}
                <NoteBox>
                    <Span
                        l10nKey="TeamCollection.Merging"
                        l10nParam0={props.collectionName}
                        temporarilyDisableI18nWarning={true}
                    >
                        You already have a collection with this same name. If
                        you continue, Bloom will merge your existing "%0"
                        collection with the Team Collection that you are
                        joining.
                    </Span>
                </NoteBox>
                {getMatchingCollection()}
            </React.Fragment>
        );
    }

    function getMatchingCollection() {
        return (
            <p>
                <Span
                    l10nKey="TeamCollection.MatchingLocal"
                    temporarilyDisableI18nWarning={true}
                >
                    Matching local collection:
                </Span>{" "}
                <span>{props.existingCollectionFolder}</span>
            </p>
        );
    }

    // This one is for an existing TC which we determine we have already joined.
    function getDialogBodyExistingTC(): JSX.Element {
        return (
            <React.Fragment>
                <NoteBox>
                    <Div
                        l10nKey="TeamCollection.AlreadyJoined"
                        temporarilyDisableI18nWarning={true}
                    >
                        This computer is already connected to this collection.
                        Bloom will open it for you.
                    </Div>
                </NoteBox>
                {getMatchingCollection()}
            </React.Fragment>
        );
    }

    // When there is no local TC already existing. Just the simple message saying Bloom will set one up.
    function getDialogBodyCreateNew(): JSX.Element {
        return <React.Fragment>{getBloomWillSetYouUp()}</React.Fragment>;
    }

    // Most of the content is common for the two pathological cases where the existing local
    // collection is a TC but NOT already linked to the one we're trying to join.
    function getConflictingCollectionCommon() {
        return (
            <React.Fragment>
                <ErrorBox>
                    <Div
                        l10nKey="TeamCollection.ConflictingCollection"
                        temporarilyDisableI18nWarning={true}
                    >
                        Bloom found another collection with this same name that
                        is already connected to a different Team Collection.
                        Click REPORT to get help from the Bloom team.
                    </Div>
                </ErrorBox>
                {getMatchingCollection()}
                <p>
                    <Span
                        l10nKey="TeamCollection.ConflictingCollection"
                        temporarilyDisableI18nWarning={true}
                    >
                        Conflicting Team collection:
                    </Span>
                    <span>{props.conflictingCollection}</span>
                </p>
            </React.Fragment>
        );
    }

    function getDialogBodyMissingTCParts() {
        return (
            <ErrorBox>
                <Div
                    l10nKey="TeamCollection.NotValidTeamCollection"
                    temporarilyDisableI18nWarning={true}
                >
                    This does not appear to be a valid Team Collection Folder.
                    Make sure you have file-syncing set up with the entire Bloom
                    Collection folder, and that the synchronization has had time
                    to complete.
                </Div>
            </ErrorBox>
        );
    }

    // Existing local local collection is a TC linked to another location.
    function getDialogBodyExistingTcElsewhere() {
        return (
            <React.Fragment>
                <NoteBox>
                    <Div
                        l10nKey="TeamCollection.AlreadyJoinedElsewhere"
                        temporarilyDisableI18nWarning={true}
                    >
                        This computer is already connected to this collection,
                        which appears to have moved. Bloom will fix things up
                        and open it for you.
                    </Div>
                </NoteBox>
                {getMatchingCollection()}
            </React.Fragment>
        );
    }

    // Existing local local collection is a TC for a different collection.
    function getDialogBodyDifferentTc() {
        return (
            <React.Fragment>
                {getConflictingCollectionCommon()}
                <P
                    l10nKey="TeamCollection.SameIds"
                    temporarilyDisableI18nWarning={true}
                >
                    (Different TC IDs)
                </P>
            </React.Fragment>
        );
    }

    function getBodyOfDialogByState(): JSX.Element {
        switch (dialogState) {
            case JoinCollectionState.IncompleteTeamCollection:
                return getDialogBodyMissingTCParts();
            case JoinCollectionState.MatchesExistingNonTeamCollection:
                return getDialogBodyExistingNonTC();
            case JoinCollectionState.MatchesExistingTeamCollection:
                return getDialogBodyExistingTC();
            case JoinCollectionState.CreateNewCollection:
                return getDialogBodyCreateNew();
            case JoinCollectionState.MatchesExistingTeamCollectionElsewhere:
                return getDialogBodyExistingTcElsewhere();
            case JoinCollectionState.MatchesDifferentTeamCollection:
                return getDialogBodyDifferentTc();
            default:
                return <div />;
        }
    }

    const wantReportButton =
        dialogState === JoinCollectionState.IncompleteTeamCollection ||
        dialogState === JoinCollectionState.MatchesDifferentTeamCollection;

    return (
        <BloomDialog {...propsForBloomDialog}>
            <DialogTitle title={`${dialogTitle} (experimental)`} />
            <DialogMiddle>{getBodyOfDialogByState()}</DialogMiddle>
            <DialogBottomButtons>
                {wantReportButton && (
                    <DialogBottomLeftButtons>
                        {props.missingTcPieces ? (
                            <DialogReportButton
                                buttonText="Ask for help"
                                l10nKey="Common.AskForHelp"
                                temporarilyDisableI18nWarning={true}
                                shortMessage="Problem joining Team Collection due to missing pieces of the collection"
                                messageGenerator={() =>
                                    // Not trying to be very nice about this message. The user will not usually see it.
                                    // It will be buried in the details of the report sent to YouTrack to tell US what went wrong.
                                    `Trying to join ${props.joiningRepo} but ${props.missingTcPieces} were not found`
                                }
                            />
                        ) : (
                            <DialogReportButton
                                shortMessage="Problem joining Team Collection due to conflicting local collection"
                                messageGenerator={() =>
                                    `trying to join ${props.joiningRepo} (${props.joiningGuid}), but local collection ${props.existingCollectionFolder} (${props.localGuid}) is linked to ${props.conflictingCollection}`
                                }
                                // It's tempting to look for a way to closeDialog() when Report is clicked;
                                // but if we do it after opening the problem dialog, it closes that instead.
                                // If we do it before opening the problem dialog, then the code to open it may get unloaded before it runs.
                                // It's also just possible that the user might want to see some information in the dialog while writing
                                // something helpful to us in the problem report dialog. So I decided not to. If we do want to do it,
                                // we probably need a new API, as well as a click handler on DialogReportButton.
                            />
                        )}
                    </DialogBottomLeftButtons>
                )}
                <BloomButton
                    l10nKey={l10nJoinButtonKey}
                    temporarilyDisableI18nWarning={true}
                    hasText={true}
                    enabled={!wantReportButton}
                    onClick={() => {
                        BloomApi.post("teamCollection/joinTeamCollection");
                    }}
                >
                    {joinButtonEnglish}
                </BloomButton>
                <DialogCancelButton onClick={closeDialog} />
            </DialogBottomButtons>
        </BloomDialog>
    );
};

WireUpForWinforms(JoinTeamCollectionDialog);
