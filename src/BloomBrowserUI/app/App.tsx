import Button from "@material-ui/core/Button";
import * as React from "react";
import "App.less";
import { CollectionsTabPane } from "../collectionsTab/CollectionsTabPane";
import { WireUpForWinforms } from "../utils/WireUpWinform";
import { kBloomBlue, kPanelBackground } from "../bloomMaterialUITheme";

// invoke this with http://localhost:8089". Doesn't do much yet... someday will be the root of our UI.

export const App: React.FunctionComponent<{}> = props => {
    return (
        <div style={{ backgroundColor: kPanelBackground, height: "100%" }}>
            <div style={{ backgroundColor: kBloomBlue, paddingTop: "3px" }}>
                <Tabs />
            </div>
            <CollectionsTabPane />
        </div>
    );
};
const Tabs: React.FunctionComponent<{}> = props => {
    return (
        <ul id="main-tabs" style={{ height: "77px" }}>
            <li>
                <Button
                    className={"selected"}
                    startIcon={<img src="../images/CollectionsTab.svg" />}
                >
                    Collections
                </Button>
                <Button startIcon={<img src="../images/EditTab.svg" />}>
                    Edit
                </Button>
                <Button startIcon={<img src="../images/PublishTab.svg" />}>
                    Publish
                </Button>
            </li>
        </ul>
    );
};

WireUpForWinforms(App);
