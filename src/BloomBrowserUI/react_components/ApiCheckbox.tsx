import * as React from "react";
import { MuiCheckbox } from "./muiCheckBox";
import { BloomApi } from "../utils/bloomApi";

// A localized checkbox that is backed by a boolean API get/set
// This is a "uncontrolled component".
export const ApiCheckbox: React.FunctionComponent<{
    english: string;
    l10nKey: string;
    l10nComment?: string;
    apiEndpoint: string;
    disabled?: boolean;
    // If defined, the checkbox should have this value when disabled,
    // whatever value we get from the API.
    forceDisabledValue?: boolean;
    onChange?: () => void;
}> = props => {
    const [checked, setChecked] = BloomApi.useApiBoolean(
        props.apiEndpoint,
        false
    );

    let showChecked = checked;
    if (props.disabled && props.forceDisabledValue !== undefined) {
        showChecked = props.forceDisabledValue;
    }

    return (
        <MuiCheckbox
            checked={showChecked}
            disabled={props.disabled}
            label={props.english}
            l10nKey={props.l10nKey}
            l10nComment={props.l10nComment}
            onCheckChanged={(newState: boolean | undefined) => {
                setChecked(!!newState);
                if (props.onChange) {
                    props.onChange();
                }
            }}
        />
    );
};
