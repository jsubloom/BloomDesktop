import { useRef } from "react";
import React = require("react");
import { postAsync, postThatMightNavigate } from "../utils/bloomApi";
import BloomButton, { IBloomButtonProps } from "./bloomButton";
import {
    IProgressDialogProps,
    ProgressDialog
} from "./Progress/ProgressDialog";

export interface IBloomButtonWithProgressProps {
    buttonProps: Omit<IBloomButtonProps, "ref">; // Need to remove 'ref" field or else Typescript if you pass these to a BloomButton. Probably because ref is there twice.
    progressDialogProps: Omit<
        IProgressDialogProps,
        "setShowDialog" | "setCloseDialog"
    >;
    delayInMilliseconds?: number;
}

export const BloomButtonWithProgress: React.FunctionComponent<IBloomButtonWithProgressProps> = props => {
    const showProgress = useRef<() => void | undefined>();
    const closeProgress = useRef<() => void | undefined>();

    const {
        onClick: onClickFromProps,
        clickApiEndpoint,
        mightNavigate,
        ...restOfButtonProps
    } = props.buttonProps;

    const onClickWithProgressDialog = async () => {
        if (!props.delayInMilliseconds) {
            // Show immediately
            showProgress.current?.();
        } else {
            // Show after a delay
            setTimeout(() => {
                showProgress.current?.();
            }, props.delayInMilliseconds);
        }

        if (onClickFromProps) {
            onClickFromProps();
        } else if (clickApiEndpoint) {
            if (mightNavigate) {
                await postThatMightNavigate(clickApiEndpoint);
            } else {
                await postAsync(clickApiEndpoint);
            }

            closeProgress.current?.();
        }
    };

    return (
        <>
            <BloomButton
                {...restOfButtonProps}
                onClick={onClickWithProgressDialog}
            />
            <ProgressDialog
                {...props.progressDialogProps}
                setShowDialog={showFunc => (showProgress.current = showFunc)}
                setCloseDialog={closeFunc =>
                    (closeProgress.current = closeFunc)
                }
            />
        </>
    );
};
