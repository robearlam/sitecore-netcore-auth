# escape=`

ARG PARENT_IMAGE
ARG SOLUTION_IMAGE

FROM ${SOLUTION_IMAGE} as solution
FROM ${PARENT_IMAGE}

# Copy solution identity files
COPY --from=solution /artifacts/identity/ ./