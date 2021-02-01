// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import {
    IsActivePackageRenderer,
    TimeRenderer,
    SoftSelectLinkRenderer,
} from "components/shared/cellRenderers";
import {
    EMPTY_FIELD_VAL,
    gridValueFormatters,
    checkboxColumn,
} from "components/shared/pcsGrid/pcsGridConfig";
import { getPackageTypeTranslation, getConfigTypeTranslation } from "utilities";
import { INACTIVE_PACKAGE_TAG } from "services/configService";
import { CopyToClipBoardRenderer } from "components/shared/cellRenderers/copyToClipBoardRenderer/copyToClipBoardRenderer";

const { checkForEmpty } = gridValueFormatters;

export const packagesColumnDefs = {
    checkBox: {
        lockPosition: checkboxColumn.lockPosition,
        cellClass: checkboxColumn.cellClass,
        headerClass: checkboxColumn.headerClass,
        suppressResize: checkboxColumn.suppressResize,
        checkboxSelection: checkboxColumn.checkboxSelection,
        headerCheckboxSelection: checkboxColumn.headerCheckboxSelection,
        headerCheckboxSelectionFilteredOnly:
            checkboxColumn.headerCheckboxSelectionFilteredOnly,
        suppressMovable: checkboxColumn.suppressMovable,
        width: 50,
    },
    id: {
        headerName: " ",
        field: "id",
        cellRendererFramework: CopyToClipBoardRenderer,
    },
    name: {
        headerName: "packages.grid.name",
        field: "name",
        sort: "asc",
        valueFormatter: ({ value }) => checkForEmpty(value),
        cellRendererFramework: SoftSelectLinkRenderer,
    },
    packageType: {
        headerName: "packages.grid.packageType",
        field: "packageType",
        valueFormatter: ({ value, context: { t } }) =>
            getPackageTypeTranslation(checkForEmpty(value), t),
    },
    configType: {
        headerName: "packages.grid.configType",
        field: "configType",
        valueFormatter: ({ value, context: { t } }) =>
            getConfigTypeTranslation(checkForEmpty(value), t),
    },
    dateCreated: {
        headerName: "packages.grid.dateCreated",
        field: "dateCreated",
        cellRendererFramework: TimeRenderer,
    },
    active: {
        headerName: "packages.grid.active",
        field: "active",
        cellRendererFramework: IsActivePackageRenderer,
    },
    tags: {
        headerName: "packages.grid.tags",
        field: "tags",
        valueFormatter: ({ value }) =>
            Object.keys(value || {})
                .filter((key) => key !== INACTIVE_PACKAGE_TAG)
                .join("; ") || EMPTY_FIELD_VAL,
    },
    version: {
        headerName: "packages.grid.version",
        field: "version",
        valueFormatter: ({ value }) => checkForEmpty(value),
    },
    lastModified: {
        headerName: "packages.grid.lastModified",
        field: "lastModified",
        cellRendererFramework: TimeRenderer,
    },
    lastModifiedBy: {
        headerName: "packages.grid.lastModifedBy",
        field: "lastModifiedBy",
        valueFormatter: ({ value }) => checkForEmpty(value),
    },
};

export const defaultPackagesGridProps = {
    enableColResize: true,
    multiSelect: true,
    pagination: true,
    paginationPageSize: Config.paginationPageSize,
    rowSelection: "multiple",
};
