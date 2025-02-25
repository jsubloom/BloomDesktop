import { BloomApi } from "../utils/bloomApi";
import * as React from "react";
import {
    ILocalizationProps,
    ILocalizationState,
    LocalizableElement
} from "./l10nComponents";
import Button from "@material-ui/core/Button";

export interface IBloomButtonProps extends ILocalizationProps {
    id?: string;
    enabled: boolean;
    clickApiEndpoint?: string;
    onClick?: () => void;
    transparent?: boolean;
    variant?: "text" | "outlined" | "contained" | undefined; // see https://material-ui.com/api/button/
    mightNavigate?: boolean; // true if the post of clickEndpoint might navigate to a new page.
    hasText: boolean; // allows us to define buttons with only images and no text.
    // If neither enabled or disabled image file is provided, no image will show.
    // If only one is provided, no image will show in the other state (e.g. if disabled and no disabledImageFile).
    enabledImageFile?: string;
    disabledImageFile?: string;
    l10nTipEnglishEnabled?: string; // existence of these two strings (or one of them) enables tooltips on the button.
    l10nTipEnglishDisabled?: string;
    iconBeforeText?: React.ReactNode;
    size?: "small" | "medium" | "large" | undefined;
    color?: "primary" | "secondary" | undefined;
}

// A button that takes a Bloom API endpoint to post() when clicked
// and a bool to determine if this button should currently be enabled.
// The button displays localizable text and optionally enabled and disabled versions of an image file.
// Also, this button optionally implements the LocalizableElement tooltip function.
export default class BloomButton extends LocalizableElement<
    IBloomButtonProps,
    ILocalizationState
> {
    constructor(props: IBloomButtonProps) {
        super(props);
    }

    private getButtonImage(): JSX.Element | null {
        if (this.props.enabled && this.props.enabledImageFile) {
            return <img src={this.props.enabledImageFile} key={0} />;
        } else if (!this.props.enabled && this.props.disabledImageFile) {
            return <img src={this.props.disabledImageFile} key={0} />;
        }
        return null;
    }

    public render() {
        const image = this.getButtonImage();
        let tip: string = "";
        if (
            this.props.l10nTipEnglishEnabled ||
            this.props.l10nTipEnglishDisabled
        ) {
            tip = this.getLocalizedTooltip(this.props.enabled);
        }
        const hidden = this.props.hidden ? "hidden" : "";
        const commonProps = {
            id: this.props.id,
            title: tip,
            onClick: () => this.onClick(),
            disabled: !this.props.enabled,
            className: this.props.className
                ? this.props.className + (hidden ? " " + hidden : "")
                : hidden
        };
        const localizedContent = this.props.hasText && (
            <span key={1}>{this.getLocalizedContent()}</span>
        );
        const commonChildren = [image, localizedContent];
        return this.props.transparent ? (
            // I don't know how to make a material-ui button transparent at the moment,
            /// so use a plain html one
            <button {...commonProps}>{commonChildren}</button>
        ) : (
            // if not transparent, then we can use Material-ui
            <Button
                {...commonProps}
                variant={this.props.variant || "contained"}
                color={this.props.color ? this.props.color : "primary"}
                startIcon={this.props.iconBeforeText}
                size={this.props.size}
                {...this.props} // allows defining more css rules from container
            >
                {commonChildren}
            </Button>
        );
    }

    private onClick() {
        if (this.props.onClick) {
            this.props.onClick();
        } else if (this.props.clickApiEndpoint) {
            if (this.props.mightNavigate) {
                BloomApi.postThatMightNavigate(this.props.clickApiEndpoint);
            } else {
                BloomApi.post(this.props.clickApiEndpoint);
            }
        }
    }
}
