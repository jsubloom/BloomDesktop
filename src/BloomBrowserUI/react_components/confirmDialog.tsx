import React = require("react");
import * as ReactDOM from "react-dom";
import { useState } from "react";
import Dialog from "@mui/material/Dialog";
import {
    DialogTitle,
    DialogActions,
    DialogContent,
    ThemeProvider,
    StyledEngineProvider
} from "@mui/material";
import CloseOnEscape from "react-close-on-escape";
import { useL10n } from "./l10nHooks";
import BloomButton from "./bloomButton";
import { getEditTabBundleExports } from "../bookEdit/js/bloomFrames";
import { postBoolean } from "../utils/bloomApi";
import { lightTheme } from "../bloomMaterialUITheme";

// All strings are assumed localized by the caller
export interface IConfirmDialogProps {
    title: string;
    titleL10nKey: string;
    message: string;
    messageL10nKey: string;
    confirmButtonLabel: string;
    confirmButtonLabelL10nKey: string;
    onDialogClose: (result: DialogResult) => void;
}

let externalSetOpen;

const ConfirmDialog: React.FC<IConfirmDialogProps> = props => {
    const [open, setOpen] = useState(true);
    externalSetOpen = setOpen;

    React.useEffect(() => {
        postBoolean("editView/setModalState", open);
    }, [open]);

    const onClose = (result: DialogResult) => {
        setOpen(false);
        props.onDialogClose(result);
    };

    return (
        <StyledEngineProvider injectFirst>
            <ThemeProvider theme={lightTheme}>
                <CloseOnEscape onEscape={() => onClose(DialogResult.Cancel)}>
                    <Dialog
                        className="bloomModalDialog confirmDialog"
                        open={open}
                    >
                        <DialogTitle>
                            {useL10n(props.title, props.titleL10nKey)}
                        </DialogTitle>
                        <DialogContent>
                            {useL10n(props.message, props.messageL10nKey)}
                        </DialogContent>
                        <DialogActions>
                            <BloomButton
                                key="Confirm"
                                l10nKey={props.confirmButtonLabelL10nKey}
                                enabled={true}
                                onClick={() => onClose(DialogResult.Confirm)}
                                hasText={true}
                            >
                                {props.confirmButtonLabel}
                            </BloomButton>
                            <BloomButton
                                key="Cancel"
                                l10nKey="Common.Cancel"
                                enabled={true}
                                onClick={() => onClose(DialogResult.Cancel)}
                                hasText={true}
                                variant="outlined"
                            >
                                Cancel
                            </BloomButton>
                        </DialogActions>
                    </Dialog>
                </CloseOnEscape>
            </ThemeProvider>
        </StyledEngineProvider>
    );
};

export enum DialogResult {
    Confirm,
    Cancel
}

export const showConfirmDialog = (
    props: IConfirmDialogProps,
    container?: Element | null
) => {
    doRender(props, container);
    externalSetOpen(true);
};

const doRender = (props: IConfirmDialogProps, container?: Element | null) => {
    let modalContainer;
    if (container) modalContainer = container;
    else modalContainer = getEditTabBundleExports().getModalDialogContainer();
    try {
        ReactDOM.render(<ConfirmDialog {...props} />, modalContainer);
    } catch (error) {
        console.error(error);
    }
};
