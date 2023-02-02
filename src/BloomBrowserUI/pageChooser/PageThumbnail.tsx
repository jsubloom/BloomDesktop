/** @jsx jsx **/
import { jsx, css } from "@emotion/react";

import * as React from "react";
import { kBloomBuff } from "../bloomMaterialUITheme";

interface IPageThumbnailProps {
    imageSource: string;
    isLandscape: boolean;
}

export const PageThumbnail: React.FunctionComponent<IPageThumbnailProps> = props => {
    const thumbFrameStyles = `margin: 0 10px 10px 11px;
        width: 104px;
        display: inline-block;
        position: sticky;`;

    const commonThumbStyles = `
            overflow: hidden;
            display: block;
            background: inherit;
            border: 1px solid ${kBloomBuff};
            margin-top: 4px;
            max-height: 85px;
            object-fit: contain;
        `;
    const orientationThumbStyles = props.isLandscape
        ? `
            width: unset;
            height: 60px;
            margin-left: 6px;
            max-width: 85px;
        `
        : `
            width: 60px;
            margin-left: 20px;
            height: unset;
        `;

    return (
        <div
            className="pageThumbnail"
            css={css`
                ${thumbFrameStyles}
            `}
        >
            <img
                css={css`
                    ${commonThumbStyles + orientationThumbStyles}
                `}
                src={props.imageSource}
            />
        </div>
    );
};

export default PageThumbnail;
