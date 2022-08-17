/** @jsx jsx **/
import { jsx, css } from "@emotion/core";

import { Button, CircularProgress } from "@material-ui/core";
import * as React from "react";
import { useEffect, useRef, useState } from "react";
import { BloomApi } from "../../utils/bloomApi";
import WebSocketManager, {
    IBloomWebSocketProgressEvent,
    useSubscribeToWebSocketForEvent,
    useSubscribeToWebSocketForObject
} from "../../utils/WebSocketManager";
import BloomButton from "../bloomButton";
import { ProgressBox } from "./progressBox";
import { kBloomGold, kErrorColor } from "../../bloomMaterialUITheme";
import {
    BloomDialog,
    DialogBottomButtons,
    DialogBottomLeftButtons,
    DialogMiddle,
    DialogTitle
} from "../BloomDialog/BloomDialog";
import {
    DialogCancelButton,
    DialogCloseButton
} from "../BloomDialog/commonDialogComponents";
import { WireUpForWinforms } from "../../utils/WireUpWinform";
import {
    IBloomDialogEnvironmentParams,
    useSetupBloomDialog
} from "../BloomDialog/BloomDialogPlumbing";

export interface IProgressDialogProps {
    title: string;
    titleColor?: string;
    titleIcon?: string;
    titleBackgroundColor?: string;
    // defaults to "never"
    showReportButton?: "always" | "if-error" | "never";
    showCancelButton?: boolean;
    onReadyToReceive?: () => void;
    // Be very careful how you use these. A typical pattern is that you want to pass a function
    // which will save the show() or close() function to use later, possibly in a listener. The temptation
    // is to save it in state, something like this:
    // const [show, setShow] = useState<()=>void|undefined>();
    // useEffect(() => something.addEventListener(..., () => show?()), []);
    // ...
    // <ProgressDialog ... setShowDialog={showFunc => setShow(showFunc)}...>
    // This looks as though it will save the function passed to setShowDialog in the "show" state
    // and use it when the event happens. But it actually fails in two different and baffling ways.
    // First, because the state is a function, when you call setShow(func), it does not save func in show;
    // it EXECUTES func immediately and saves the result, with the result that the progress dialog
    // mysteriously appears at once. (When the argument to a state setter is a function, it is
    // assumed to be an update function.) This could be fixed by passing a function that returns showFunc:
    // <ProgressDialog ... setShowDialog={showFunc => setShow(() => showFunc)}...>.
    // Now, () => showFunc is executed immediately, and the result (showFunc) is saved as we want...almost...
    // But this still fails; bafflingly, all logging and the debugger show that "show" is a function, but
    // in the listener it remains undefined. The listener was set up in the first render, when "show"
    // had not yet been set! It "captured" the initial state.
    // The right pattern is
    // const show = useRef<()=>void|undefined>();
    // useEffect(() => something.addEventListener(..., () => show.current?()), []);
    // ...
    // <ProgressDialog ... setShowDialog={showFunc => show.current = showFunc}...>
    setShowDialog?: (show: () => void) => void;
    setCloseDialog?: (close: () => void) => void;
    dialogEnvironment?: IBloomDialogEnvironmentParams;
}

interface IEmbeddedProgressProps extends IProgressDialogProps {
    which: string; // must match props.which to open
}

export const ProgressDialog: React.FunctionComponent<IProgressDialogProps> = props => {
    const {
        showDialog,
        closeDialog,
        propsForBloomDialog
    } = useSetupBloomDialog(props.dialogEnvironment);
    if (props.setShowDialog) {
        props.setShowDialog(showDialog);
    }
    if (props.setCloseDialog) {
        props.setCloseDialog(closeDialog);
    }
    const [showButtons, setShowButtons] = useState(false);
    const [sawAnError, setSawAnError] = useState(false);
    const [sawAWarning, setSawAWarning] = useState(false);
    const [sawFatalError, setSawFatalError] = useState(false);
    const [messagesForErrorReporting, setMessagesForErrorReporting] = useState(
        ""
    );
    const [messages, setMessages] = React.useState<Array<JSX.Element>>([]);

    const [listenerReady, setListenerReady] = useState(false);
    const [progressBoxReady, setProgressBoxReady] = useState(false);
    useEffect(() => {
        if (
            listenerReady &&
            progressBoxReady &&
            props.onReadyToReceive &&
            propsForBloomDialog.open
        ) {
            props.onReadyToReceive();
        }
    }, [listenerReady, progressBoxReady, propsForBloomDialog.open]);

    // Start off showing the spinner, then stop when we get a "finished" message.
    const [showSpinner, setShowSpinner] = useState(true);

    // Note that the embedded ProgressBox is also listening to the same stream of events.
    // Here we are just concerned with events that change the state of our buttons, title bar, etc.
    React.useEffect(() => {
        const listener = (e: IBloomWebSocketProgressEvent) => {
            if (e.id === "message") {
                setMessagesForErrorReporting(
                    current => current + "\r\n" + e.message
                );
            }
            if (e.id === "show-buttons") {
                setShowButtons(true);
            }
            if (e.id === "message" && e.progressKind === "Error") {
                setSawAnError(true);
            }
            if (e.id === "message" && e.progressKind === "Fatal") {
                setSawAnError(true);
                setSawFatalError(true);
            }
            if (e.id === "message" && e.progressKind === "Warning") {
                setSawAWarning(true);
            }
            if (e.id === "finished") {
                setShowSpinner(false);
            }
        };
        WebSocketManager.addListener("progress", listener);
        setListenerReady(true);
        // cleanup when this dialog unmounts (since this useEffect will only be called once)
        return () => {
            WebSocketManager.removeListener("progress", listener);
        };
    }, []);

    // Any time we are closed and reopened we want to show a new set of messages.
    const everOpened = useRef(false); // we don't need, or want, a change to this to trigger a render.
    useEffect(() => {
        if (propsForBloomDialog.open) {
            everOpened.current = true;
        } else {
            // Once the dialog has been open, the only way this effect runs again is if it
            // it's open state changes. But we don't want this to happen on the initial
            // render, when it is closed and has never been open.
            // (In particular, the post might have unintended consequences. But the other
            // stuff will at least cause useless renders.)
            // (We don't really need to clean it up until it opens again. But cleaning
            // when it closes should free some memory and may help to prevent the old
            // messages flickering into view  when it reopens. Also, the re-renders
            // caused by the changed state are probably less expensive when it is closed.)
            if (everOpened.current) {
                setMessages([]);
                setSawAnError(false);
                setSawAWarning(false);
                setSawFatalError(false);
                setShowButtons(false);
                BloomApi.post("progress/closed");
            }
        }
    }, [propsForBloomDialog.open]);

    const buttonForSendingErrorReportIsRelevant =
        props.showReportButton == "always" ||
        (sawAnError && props.showReportButton == "if-error");

    let titleColor = props.titleColor || "black";
    let titleBackground = props.titleBackgroundColor || "transparent";
    if (sawAWarning) {
        titleBackground = kBloomGold;
        titleColor = "black";
    }
    if (sawAnError) {
        titleBackground = kErrorColor;
        titleColor = "white";
    }

    return (
        <BloomDialog {...propsForBloomDialog}>
            <DialogTitle
                title={props.title}
                icon={props.titleIcon}
                backgroundColor={titleBackground}
                color={titleColor}
            >
                {showSpinner && (
                    <CircularProgress
                        css={css`
                            margin-left: auto;
                            margin-top: auto;
                            margin-bottom: auto;
                            color: ${props.titleColor || "black"} !important;
                        `}
                        size={20}
                    />
                )}
            </DialogTitle>
            <DialogMiddle
                css={css`
                    // I don't actually understand why I had do to this, other than than
                    // I'm hopeless at css sizing stuff. See storybook story ProgressDialog: long.
                    overflow-y: ${propsForBloomDialog.dialogFrameProvidedExternally
                        ? "auto"
                        : "unset"};
                `}
            >
                <ProgressBox
                    webSocketContext="progress"
                    onReadyToReceive={() => setProgressBoxReady(true)}
                    css={css`
                        // If we have dialogFrameProvidedExternally that means the dialog height is controlled by c#, so let the progress grow to fit it.
                        // Maybe we could have that approach *all* the time?
                        height: ${props.dialogEnvironment
                            ?.dialogFrameProvidedExternally
                            ? "100%"
                            : "400px"};
                        min-width: 540px;
                    `}
                    // This is utterly bizarre. When not wrapped in a material UI Dialog, ProgressBox happily
                    // keeps track of its own messages. But Dialog repeatedly mounts and unmounts its children,
                    // for no reason I can discover, resulting in loss of their state. So we must keep any state we need
                    // outside the Dialog wrapper.
                    messages={messages}
                    setMessages={setMessages}
                />
            </DialogMiddle>
            <DialogBottomButtons>
                {showButtons ? (
                    <React.Fragment>
                        {buttonForSendingErrorReportIsRelevant && (
                            <DialogBottomLeftButtons>
                                <BloomButton
                                    id="progress-report"
                                    hasText={true}
                                    enabled={true}
                                    //color="primary"
                                    l10nKey="Common.Report"
                                    variant="text"
                                    onClick={() => {
                                        BloomApi.postJson(
                                            "problemReport/showDialog",
                                            {
                                                message: messagesForErrorReporting,
                                                shortMessage: `The user reported a problem from "${props.title}".`
                                            }
                                        );
                                    }}
                                >
                                    Report
                                </BloomButton>
                            </DialogBottomLeftButtons>
                        )}
                        {sawFatalError ? (
                            <BloomButton
                                l10nKey="ReportProblemDialog.Quit"
                                hasText={true}
                                enabled={true}
                                variant="contained"
                                temporarilyDisableI18nWarning={true} // only used in TC context so far
                                onClick={closeDialog}
                            >
                                Quit
                            </BloomButton>
                        ) : (
                            <DialogCloseButton onClick={closeDialog} />
                        )}
                    </React.Fragment>
                ) : // if we're not done, show the cancel button if that was called for...
                props.showCancelButton ? (
                    <DialogCancelButton
                        onClick={() => {
                            BloomApi.post("progress/cancel");
                        }}
                    />
                ) : (
                    // ...otherwise show a placeholder to leave room for buttons when the progress is over
                    <Button
                        variant="contained"
                        css={css`
                            visibility: hidden;
                        `}
                    >
                        placeholder
                    </Button>
                )}
            </DialogBottomButtons>
        </BloomDialog>
    );
};

// Simply render one of these, with no props, at the top level of any document where the
// C# code (or possibly one day JS code??) might want to show a progress dialog. Showing the
// dialog and sending stuff to it is all managed by websocket events, and events initiated
// by buttons in the dialog result in posts. It occupies no space and is invisible until
// an event tells it to show up. See C# BrowserProgressDialog.
export const EmbeddedProgressDialog: React.FunctionComponent<{
    id: string;
}> = props => {
    const [progressProps, setProgressProps] = useState<IEmbeddedProgressProps>({
        which: "",
        // just lets us know something is wrong if it shows up; a real title should
        // be supplied by the code that causes it to become visible.
        title: "This should not be seen",
        dialogEnvironment: {
            initiallyOpen: false,
            dialogFrameProvidedExternally: false
        }
    });
    // Used to store the functions that we get through ProgressDialog props callback functions
    // for showing and closing the progress dialog.
    const showProgress = useRef<() => void | undefined>();
    const closeProgress = useRef<() => void | undefined>();
    const openProgress = (args: IEmbeddedProgressProps) => {
        if (args.which !== props.id) {
            return; // message for another progress dialog, typically in another browser instance
        }
        // Note, we can't get the dialog shown here by setting props to
        // something with dialogEnvironment having initiallyOpen true;
        // that initial value gets captured on the first render. We have to capture
        // the function which the ProgressDialog hands out for showing itself.
        // args are sent from the C# code that wants to open the dialog.
        setProgressProps({
            ...progressProps,
            ...args
        });
        showProgress.current?.(); // better not be null, but lint insists
    };
    useSubscribeToWebSocketForObject(
        "progress",
        "open-progress",
        (args: IEmbeddedProgressProps) => openProgress(args)
    );
    useSubscribeToWebSocketForEvent("progress", "close-progress", () =>
        closeProgress.current?.()
    );
    return (
        <ProgressDialog
            {...progressProps}
            setShowDialog={showFunc => (showProgress.current = showFunc)}
            setCloseDialog={closeFunc => (closeProgress.current = closeFunc)}
            onReadyToReceive={() => BloomApi.post("progress/ready")}
        />
    );
};

WireUpForWinforms(ProgressDialog);
