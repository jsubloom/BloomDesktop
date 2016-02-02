﻿/// <reference path="../toolbox.ts" />
import { ReaderToolsModel, DRTState, } from "../decodableReader/readerToolsModel";
import { initializeLeveledReaderTool} from "../decodableReader/readerTools";
import {ITabModel} from "../toolbox";
import {ToolBox} from "../toolbox";
import {theOneLibSynphony}  from '../decodableReader/libSynphony/synphony_lib';

class LeveledReaderModel implements ITabModel {
    restoreSettings(opts: string) {
        if (!ReaderToolsModel.model) ReaderToolsModel.model = new ReaderToolsModel();
        initializeLeveledReaderTool();
        if (opts['leveledReaderState']) {
            var state = theOneLibSynphony.dbGet('drt_state');
            if (!state) state = new DRTState();
            state.level = parseInt(opts['leveledReaderState']);
            theOneLibSynphony.dbSet('drt_state', state);
        }
    }

    configureElements(container: HTMLElement) {}

    showTool() {
        // change markup based on visible options
        ReaderToolsModel.model.setCkEditorLoaded(); // we don't call showTool until it is.
        if (!ReaderToolsModel.model.setMarkupType(2)) ReaderToolsModel.model.doMarkup();
    }

    hideTool() {
        ReaderToolsModel.model.setMarkupType(0);
    }

    updateMarkup() {
        ReaderToolsModel.model.doMarkup();
    }

    name() {return 'leveledReaderTool';}
}

ToolBox.getTabModels().push(new LeveledReaderModel());