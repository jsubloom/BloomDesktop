var path = require("path");
const { merge } = require("webpack-merge");
var pathToOriginalJavascriptFilesInLib = path.resolve(__dirname, "lib");
var pathToBookEditJS = path.resolve(__dirname, "bookEdit/js");
var pathToOriginalJavascriptFilesInModified_Libraries = path.resolve(
    __dirname,
    "modified_libraries"
);
var globule = require("globule");

//note: if you change this, change it in gulpfile.js & karma.conf.js as well
var outputDir = "../../output/browser";
const core = require("./webpack.core.js");

// Because our output directory does not have the same parent as our node_modules, we
// need to resolve the babel related presets (and plugins).  This mapping function was
// suggested at https://github.com/babel/babel-loader/issues/166.
function localResolve(preset) {
    return Array.isArray(preset)
        ? [require.resolve(preset[0]), preset[1]]
        : require.resolve(preset);
}

module.exports = merge(core, {
    // mode must be set to either "production" or "development" in webpack 4.
    // Webpack-common is intended to be 'required' by something that provides that.
    context: __dirname,
    //Bloom is not (yet) one webapp; it's actually a several loosely related ones.
    //So we have multiple "entry points" that we need to emit. Fortunately the webpack 4
    //optimization.splitChunks extracts the code that is common to more than one into "commonBundle.js".
    // The root file for each bundle should import errorHandler.ts to enable Bloom's custom
    // error handling for that web page.
    entry: {
        editTabBundle: "./bookEdit/editViewFrame.ts",
        readerSetupBundle:
            "./bookEdit/toolbox/readers/readerSetup/readerSetup.ts",
        editablePageBundle: "./bookEdit/editablePage.ts",
        bookPreviewBundle:
            "./collectionsTab/collectionsTabBookPane/bookPreview.ts",
        toolboxBundle: "./bookEdit/toolbox/toolboxBootstrap.ts",
        pageChooserBundle: "./pageChooser/page-chooser.ts",
        pageThumbnailListBundle:
            "./bookEdit/pageThumbnailList/pageThumbnailList.tsx",
        pageControlsBundle:
            "./bookEdit/pageThumbnailList/pageControls/pageControls.tsx",
        publishUIBundle: globule.find([
            "./publish/**/*.tsx",
            "!./publish/**/stories.tsx"
        ]),
        enterpriseSettingsBundle: "./collection/enterpriseSettings.tsx",

        performanceLogBundle: "./performance/PerformanceLogPage.tsx",
        appBundle: "./app/App.tsx",
        testBundle: globule.find([
            "./bookEdit/**/*Spec.ts",
            "./bookEdit/**/*Spec.js",
            "./lib/**/*Spec.ts",
            "./lib/**/*Spec.js",
            "./publish/**/*Spec.ts",
            "./publish/**/*Spec.js"
        ]),

        // These work with c# ReactControl:
        problemReportBundle: "./problemDialog/ProblemDialog.tsx",
        defaultBookshelfControlBundle:
            "./react_components/DefaultBookshelfControl.tsx",
        progressDialogBundle: "./react_components/Progress/ProgressDialog.tsx",
        problemReportBundle: "./problemDialog/ProblemDialog.tsx",
        createTeamCollectionDialogBundle:
            "./teamCollection/CreateTeamCollection.tsx",
        teamCollectionDialogBundle: "./teamCollection/TeamCollectionDialog.tsx",
        teamCollectionSettingsBundle:
            "./teamCollection/TeamCollectionSettingsPanel.tsx",
        joinTeamCollectionDialogBundle:
            "./teamCollection/JoinTeamCollectionDialog.tsx",
        autoUpdateSoftwareDlgBundle:
            "./react_components/AutoUpdateSoftwareDialog.tsx",

        collectionsTabPaneBundle: "./collectionsTab/CollectionsTabPane.tsx",

        // this is here for the "legacy" collections tab, though it's actually new for 5.1
        // we decided that the "legacy" bit we are trying to preserve a bit longer is the left side, the list of books.
        collectionsTabBookPaneBundle:
            "./collectionsTab/collectionsTabBookPane/CollectionsTabBookPane.tsx",

        legacyBookPreviewBundle:
            "./collectionsTab/collectionsTabBookPane/bookPreview.ts"

        //             testBundle: globule.find(["./**/*Spec.ts", "./**/*Spec.js", "!./node_modules/**"])//This slowed down webpack a ton, becuase the way it works is that it 1st it finds it all, then it excludes node_modules
    },

    output: {
        path: path.join(__dirname, outputDir),
        filename: "[name].js",

        libraryTarget: "var",

        // Makes a single entry point module's exports accessible via Exports.
        library: "[name]"
    },
    resolve: {
        // For some reason, webpack began to complain about being given minified source.
        // alias: { x
        //   "react-dom": pathToReactDom,
        //   react: pathToReact // the point of this is to use the minified version. https://christianalfoni.github.io/react-webpack-cookbook/Optimizing-rebundling.html
        // },
        modules: [
            ".",
            pathToOriginalJavascriptFilesInLib,
            "node_modules",
            pathToBookEditJS,
            pathToOriginalJavascriptFilesInModified_Libraries
        ],
        extensions: [".js", ".jsx", ".ts", ".tsx"] //We may need to add .less here... otherwise maybe it will ignore them unless they are require()'d
    },
    optimization: {
        minimize: false,
        namedModules: true,
        splitChunks: {
            cacheGroups: {
                default: false,
                commons: {
                    name: "commonBundle",
                    chunks: "initial",
                    // Our build process creates multiple independent bundle files.  (See exports.entry
                    // above.)  minChunks specifies how many of those bundles must contain a common
                    // chunk for that common chunk to be moved into commonBundle.js.  The default
                    // value 1 moves everything to a massive commonBundle.js, leaving only a small
                    // stub for each of the 10 original bundle files.  Specifying 10 creates the
                    // smallest commonBundle.js file, which is 6% smaller than the file created for
                    // webpack 1 using the old CommonChunkPlugin.  Specifying 9 (or 7 or 8) creates
                    // a 17% bigger commonBundle file at the cost of the smallest original bundle
                    // having access to some code it doesn't use.  This seemed like a good tradeoff.
                    minChunks: 9,
                    // This is the default value for minSize, the minimum size of a chunk to move
                    // to commonBundle.js.  Changing it didn't seem to have any effect in our build
                    // process.
                    minSize: 30000,
                    reuseExistingChunk: true
                }
            }
        }
    },
    module: {
        rules: [
            {
                // For the most part, we're using typescript and ts-loader handles that.
                // But for things that are still in javascript, the following babel setup allows newer
                // javascript features by compiling to the version JS feature supported by the specific
                // version of FF we currently ship with.
                test: /\.(js|jsx)$/,
                exclude: [
                    /node_modules/,
                    /ckeditor/,
                    /jquery-ui/,
                    /-min/,
                    /qtip/,
                    /xregexp-all-min.js/
                ],
                use: [
                    {
                        loader: "babel-loader",
                        query: {
                            presets: [
                                // Ensure that we target our version of geckofx (mozilla/firefox)
                                [
                                    "@babel/preset-env",
                                    {
                                        targets: {
                                            browsers: [
                                                "Firefox >= 45",
                                                "last 2 versions"
                                            ]
                                        }
                                    }
                                ],
                                "@babel/preset-react"
                            ].map(localResolve)
                        }
                    }
                ]
            },
            // WOFF Font
            {
                test: /\.woff(\?v=\d+\.\d+\.\d+)?$/,
                use: {
                    loader: "url-loader",
                    options: {
                        limit: 10000,
                        mimetype: "application/font-woff"
                    }
                }
            }
        ]
    }
});
