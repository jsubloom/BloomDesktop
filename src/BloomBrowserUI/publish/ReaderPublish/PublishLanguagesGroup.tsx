import * as React from "react";
import { useL10n } from "../../react_components/l10nHooks";
import { BloomApi } from "../../utils/bloomApi";
import {
    LangCheckboxValue,
    LanguageSelectionSettingsGroup
} from "./LanguageSelectionSettingsGroup";
import "./PublishLanguagesGroup.less";

// NOTE: Must correspond to C#"s LanguagePublishInfo
export interface ILanguagePublishInfo {
    code: string;
    name: string;
    complete: boolean;
    includeText: boolean;
    containsAnyAudio: boolean;
    includeAudio: boolean;
}

class LanguagePublishInfo implements ILanguagePublishInfo {
    public code: string;
    public name: string;
    public complete: boolean;
    public includeText: boolean;
    public containsAnyAudio: boolean;
    public includeAudio: boolean;

    public constructor(other?: ILanguagePublishInfo | undefined) {
        if (!other) {
            // Default constructor.
            // Nothing needs to happen right now.
        } else {
            // Copy constructor
            this.code = other.code;
            this.name = other.name;
            this.complete = other.complete;
            this.includeText = other.includeText;
            this.containsAnyAudio = other.containsAnyAudio;
            this.includeAudio = other.includeAudio;
        }
    }
}

// Component that shows a check box for each language in the book, allowing the user to
// control which of them to include in the published book.
export const PublishLanguagesGroup: React.FunctionComponent<{
    onChange?: () => void;
}> = props => {
    const initialValue: ILanguagePublishInfo[] = [];
    const [langs, setLangs] = React.useState(initialValue);
    React.useEffect(() => {
        BloomApi.get(
            "publish/android/languagesInBook",

            // onSuccess
            result => {
                let newLangs = result.data;
                // This is for debugging. When all is well, the JSON gets parsed automatically.
                // If there's a syntax error in the JSON, result.data is just the string.
                // Trying to parse it ourselves at least gets the syntax error into our log/debugger.
                if (!newLangs.map) {
                    newLangs = JSON.parse(newLangs);
                }

                // Note that these are just simple objects with fields, not instances of classes with methods.
                // That's why these are ILanguagePublishInfo's (interface) instead of LanguagePublishInfo's (class)
                setLangs(newLangs as ILanguagePublishInfo[]);
            }

            // onError
            // Currently just ignoring errors... letting BloomServer take care of reporting anything that comes up
            // () => {
            // }
        );
    }, []);

    const checkboxValuesForTextLangs = langs.map(item => {
        return {
            code: item.code,
            name: item.name,
            warnIncomplete: !item.complete,
            isEnabled: true,
            isChecked: item.includeText
        };
    });

    const checkboxValuesForAudioLangs = langs.map(item => {
        return {
            code: item.code,
            name: item.name,
            warnIncomplete: false, // Only show for text checkboxes
            isEnabled: item.includeText && item.containsAnyAudio,
            isChecked:
                item.includeText && item.containsAnyAudio && item.includeAudio
        };
    });

    const onLanguageUpdated = (
        item: LangCheckboxValue,
        newState: boolean,
        fieldToUpdate: string
    ) => {
        setLangs(
            langs.map(lang => {
                if (lang.code === item.code) {
                    const newLangObj = new LanguagePublishInfo(lang);
                    newLangObj[fieldToUpdate] = newState;

                    BloomApi.post(
                        `publish/android/includeLanguage?langCode=${newLangObj.code}&${fieldToUpdate}=${newState}`
                    );

                    return newLangObj;
                } else {
                    return lang;
                }
            })
        );

        if (props.onChange) {
            props.onChange();
        }
    };

    return (
        <div>
            <LanguageSelectionSettingsGroup
                label={useL10n(
                    "Text Languages",
                    "PublishTab.Android.TextLanguages"
                )}
                langCheckboxValues={checkboxValuesForTextLangs}
                onChange={(item, newState: boolean) => {
                    onLanguageUpdated(item, newState, "includeText");
                }}
            />
            <LanguageSelectionSettingsGroup
                label={useL10n(
                    "Talking Book Languages",
                    "PublishTab.Android.TalkingBookLanguages"
                )}
                langCheckboxValues={checkboxValuesForAudioLangs}
                onChange={(item, newState: boolean) => {
                    onLanguageUpdated(item, newState, "includeAudio");
                }}
            />
        </div>
    );
};
